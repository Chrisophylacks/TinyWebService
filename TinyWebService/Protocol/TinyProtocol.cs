using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using TinyWebService.Reflection;

namespace TinyWebService.Protocol
{
    internal static class TinyProtocol
    {
        public const string InstanceIdParameterName = "~i";
        public const string CallbackIdParameterName = "~c";
        public const string MetadataPath = "~meta";

        private static readonly ConcurrentDictionary<Type, object> ExistingSerializers = new ConcurrentDictionary<Type, object>();

        public static string CreatePrefix(string hostname, int port, string name)
        {
            return string.Format("http://{0}:{1}/{2}/", hostname ?? "localhost", port, name);
        }

        public static bool IsSerializableType(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(void) || type.IsEnum || ExistingSerializers.ContainsKey(type))
            {
                return true;
            }

            if (type.IsClass && type.IsDefined(typeof (DataContractAttribute), true))
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

                ExistingSerializers.TryAdd(typeof(T), this);
            }

            private Serializer(Func<T, string> serialize, Func<string, T> deserialize)
            {
                _serialize = serialize;
                _deserialize = deserialize;

                ExistingSerializers.TryAdd(typeof(T), this);
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

                if (value.Type.IsClass && value.Type.IsDefined(typeof(DataContractAttribute), true))
                {
                    return Expression.Call(typeof(Serializer<T>).GetMethod("SerializeClass", BindingFlags.NonPublic | BindingFlags.Static), value);
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

                if (targetType.IsClass && targetType.IsDefined(typeof (DataContractAttribute), true))
                {
                    return Expression.Call(typeof (Serializer<T>).GetMethod("DeserializeClass", BindingFlags.NonPublic | BindingFlags.Static), value);
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

            private static string SerializeClass(T instance)
            {
                using (var stream = new MemoryStream())
                {
                    new DataContractJsonSerializer(typeof (T)).WriteObject(stream, instance);
                    var size = (int)stream.Position;
                    stream.Seek(0, SeekOrigin.Begin);
                    return Encoding.UTF8.GetString(stream.GetBuffer(), 0, size);
                }
            }

            private static T DeserializeClass(string value)
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
                {
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
                }
            }
        }
    }
}
