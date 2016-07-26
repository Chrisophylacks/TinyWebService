using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TinyWebService.Reflection;

namespace TinyWebService.Protocol
{
    internal static class TinyProtocol
    {
        public const string InstanceIdParameterName = "~i";
        public const string CallbackIdParameterName = "~c";
        public const string MetadataPath = "~meta";

        private static readonly IDictionary<Type, object> ExistingSerializers = new Dictionary<Type, object>();

        public static string CreatePrefix(string hostname, int port, string name)
        {
            return string.Format("http://{0}:{1}/{2}/", hostname ?? "localhost", port, name);
        }

        public static string CreatePrefixFromEndpoint(string endpoint)
        {
            return "http://" + endpoint;
        }

        public static string CreateEndpoint(string hostname, int port)
        {
            return string.Format("{0}:{1}/{2}/", hostname ?? "localhost" , port, Guid.NewGuid().ToString("N"));
        }

        public static bool IsSerializableType(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(void) || type.IsEnum || ExistingSerializers.ContainsKey(type))
            {
                return true;
            }

            var itemType = type.TryGetCollectionItemType();
            if (itemType != null)
            {
                return IsSerializableType(itemType);
            }

            return false;
        }

        public static bool IsRemotableType(Type type)
        {
            return type.IsInterface;
        }

        public static Expression Serialize(this Expression expression)
        {
            return Expression.Call(typeof (Serializer<>).MakeGenericType(expression.Type).GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static), expression);
        }

        public static Expression Deserialize(this Expression expression, Type targetType)
        {
            return Expression.Call(typeof(Serializer<>).MakeGenericType(targetType).GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static), expression);
        }

        public sealed class Serializer<T>
        {
            private static Serializer<T> _instance;

            private readonly Func<T, string> _serialize;
            private readonly Func<string, T> _deserialize;

            private Serializer()
            {
                var value = Expression.Parameter(typeof(T));
                _serialize = Expression.Lambda<Func<T, string>>(SerializeExpression(value), value).Compile();

                value = Expression.Parameter(typeof(string));
                _deserialize = Expression.Lambda<Func<string, T>>(DeserializeExpression(value, typeof(T)), value).Compile();

                ExistingSerializers.Add(typeof(T), this);
            }

            private Serializer(Func<T, string> serialize, Func<string, T> deserialize)
            {
                _serialize = serialize;
                _deserialize = deserialize;

                ExistingSerializers.Add(typeof(T), this);
            }

            public static Serializer<T> Instance => _instance ?? (_instance = new Serializer<T>());

            public static void RegisterCustom(Func<T, string> serialize, Func<string, T> deserialize)
            {
                _instance = new Serializer<T>(serialize, deserialize);
            }

            public static string Serialize(T value) => Instance._serialize(value);

            public static T Deserialize(string value) => Instance._deserialize(value);

            private static Expression SerializeExpression(Expression value)
            {
                if (value.Type == typeof(void))
                {
                    return Expression.Block(value, Expression.Constant(string.Empty));
                }

                if (value.Type == typeof(string))
                {
                    return value;
                }

                if (value.Type.IsPrimitive)
                {
                    return Expression.Call(
                        null,
                        typeof(Convert).GetMethod("ToString", BindingFlags.Public | BindingFlags.Static, null, new[] { value.Type, typeof(IFormatProvider) }, null),
                        value,
                        Expression.Property(null, typeof(CultureInfo).GetProperty("InvariantCulture")));
                }

                if (value.Type.IsEnum)
                {
                    return Expression.Call(value, value.Type.GetMethod("ToString", Type.EmptyTypes));
                }

                var itemType = value.Type.TryGetCollectionItemType();
                if (itemType != null)
                {
                    return Expression.Call(typeof(Serializer<>).MakeGenericType(itemType).GetMethod("SerializeCollection", BindingFlags.NonPublic | BindingFlags.Static), value);
                }

                throw new Exception("cannot serialize expression of type " + value.Type);
            }

            private static Expression DeserializeExpression(Expression value, Type targetType)
            {
                if (targetType == typeof(void))
                {
                    return Expression.Block(typeof(void), value);
                }

                if (targetType == typeof(string))
                {
                    return value;
                }

                if (targetType.IsPrimitive)
                {
                    return Expression.Call(
                        null,
                        typeof(Convert).GetMethod("To" + targetType.Name, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(IFormatProvider) }, null),
                        value,
                        Expression.Property(null, typeof(CultureInfo).GetProperty("InvariantCulture")));
                }

                if (targetType.IsEnum)
                {
                    return Expression.Convert(
                        Expression.Call(
                            typeof (Enum).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof (Type), typeof (string), typeof (bool) }, null),
                            Expression.Constant(targetType),
                            value,
                            Expression.Constant(true)),
                        targetType);
                }

                var itemType = targetType.TryGetCollectionItemType();
                if (itemType != null)
                {
                    var enumerable = Expression.Call(typeof(Serializer<>).MakeGenericType(itemType).GetMethod("DeserializeCollection", BindingFlags.NonPublic | BindingFlags.Static), value);
                    if (targetType.IsArray)
                    {
                        return Expression.Call(typeof (Enumerable).GetMethod("ToArray").MakeGenericMethod(itemType), enumerable);
                    }
                    return enumerable;
                }

                throw new Exception("cannot deserialize expression to type " + targetType);
            }

            private static string SerializeCollection(IEnumerable<T> collection)
            {
                return "[" + string.Join(",", collection.Select(Instance._serialize)) + "]";
            }

            private static IList<T> DeserializeCollection(string value)
            {
                return value.Substring(1, value.Length - 2).Split(',').Select(Instance._deserialize).ToList();
            }
        }
    }
}
