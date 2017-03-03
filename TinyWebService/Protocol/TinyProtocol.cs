using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using TinyWebService.Client;
using TinyWebService.Infrastructure;
using TinyWebService.Reflection;

namespace TinyWebService.Protocol
{
    internal static class TinyProtocol
    {
        public const string InstanceIdParameterName = "~i";
        public const string MetadataPath = "~meta";
        public const string DetachCommand = "~detach";

        private static readonly ConcurrentDictionary<Type, object> ExistingSerializers = new ConcurrentDictionary<Type, object>();
        private static readonly ConcurrentDictionary<Type, MethodInfo> CustomFactories = new ConcurrentDictionary<Type, MethodInfo>();

        public static string CreatePrefix(string hostname, int port, string name)
        {
            return string.Format("http://{0}:{1}/{2}/", hostname ?? "localhost", port, name);
        }

        public static TypeFlags Check(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(void) || type.IsEnum || ExistingSerializers.ContainsKey(type))
            {
                return TypeFlags.DataType;
            }

            var itemType = type.TryGetCollectionItemType();
            if (itemType != null)
            {
                return Check(itemType) & TypeFlags.DataType;
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return Check(nullableType) & TypeFlags.DataType;
            }

            if (type.IsGenericType && CustomFactories.ContainsKey(type.GetGenericTypeDefinition()))
            {
                return TypeFlags.ProxyType;
            }

            if (type.IsInterface || CustomFactories.ContainsKey(type))
            {
                return TypeFlags.ProxyType;
            }

            if (type.IsClass)
            {
                if (type.IsDefined(typeof (DataContractAttribute), true))
                {
                    return TypeFlags.DataType;
                }
                return TypeFlags.DispatcherType;
            }

            return TypeFlags.None;
        }

        public static Expression Serialize(this Expression expression, Expression endpoint)
        {
            return Expression.Call(typeof (Serializer<>).MakeGenericType(expression.Type).GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static), endpoint, expression);
        }

        public static Expression Deserialize(this Expression expression, Expression endpoint, Type targetType)
        {
            return Expression.Call(typeof(Serializer<>).MakeGenericType(targetType).GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static), endpoint, expression);
        }

        public static void RegisterCustomProxyFactory<TProxyFactory>()
            where TProxyFactory : class
        {
            foreach (var method in typeof(TProxyFactory).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                CustomFactories[method.ReturnType.IsGenericType ? method.ReturnType.GetGenericTypeDefinition() : method.ReturnType] = method;
            }
        }

        public static bool TryGetCustomProxyFactory(Type interfaceType, out MethodInfo customFactory)
        {
            if (interfaceType.IsGenericType)
            {
                MethodInfo customFactoryPrototype;
                if (CustomFactories.TryGetValue(interfaceType.GetGenericTypeDefinition(), out customFactoryPrototype))
                {
                    customFactory = customFactoryPrototype.MakeGenericMethod(interfaceType.GetGenericArguments());
                    return true;
                }
            }
            else
            {
                return CustomFactories.TryGetValue(interfaceType, out customFactory);
            }

            customFactory = null;
            return false;
        }

        internal static ProxyBase GetRealProxy(object proxyObject)
        {
            var realProxy = proxyObject as ProxyBase;
            if (realProxy == null)
            {
                var remotable = proxyObject as IRemotableInstance;
                if (remotable != null)
                {
                    realProxy = remotable.RealProxy as ProxyBase;
                }
            }

            return realProxy;
        }

        internal static string SerializeRemotableInstance(IEndpoint endpoint, object proxyObject)
        {
            if (proxyObject == null)
            {
                return string.Empty;
            }

            var realProxy = GetRealProxy(proxyObject);
            if (realProxy != null)
            {
                return realProxy.Address.Encode();
            }

            return endpoint.RegisterInstance(proxyObject);
        }

        public sealed class Serializer<T>
        {
            private static Serializer<T> _instance;

            private readonly Func<IEndpoint, T, string> _serialize;
            private readonly Func<IEndpoint, string, T> _deserialize;

            private Serializer()
            {
                var endpoint = Expression.Parameter(typeof (IEndpoint));
                var value = Expression.Parameter(typeof(T));
                _serialize = Expression.Lambda<Func<IEndpoint, T, string>>(SerializeExpression(endpoint, value), endpoint, value).Compile();

                value = Expression.Parameter(typeof(string));
                var deserializeExpression = Expression.Lambda<Func<IEndpoint, string, T>>(DeserializeExpression(endpoint, value, typeof (T)), endpoint, value);
                _deserialize = deserializeExpression.Compile();

                ExistingSerializers.TryAdd(typeof(T), this);
            }

            private Serializer(Func<T, string> serialize, Func<string, T> deserialize)
            {
                _serialize = (e, v) => serialize(v);
                _deserialize = (e, v) => deserialize(v);

                ExistingSerializers.TryAdd(typeof(T), this);
            }

            public static Serializer<T> Instance 
            {
                get
                {
                    return _instance ?? (_instance = new Serializer<T>());
                }
            }

            public static void RegisterCustom(Func<T, string> serialize, Func<string, T> deserialize)
            {
                _instance = new Serializer<T>(serialize, deserialize);
            }

            public static string Serialize(IEndpoint endpoint, T value)
            {
                return Instance._serialize(endpoint, value);
            }

            public static T Deserialize(IEndpoint endpoint, string value)
            {
                return Instance._deserialize(endpoint, value);
            }

            private static Expression SerializeExpression(Expression endpoint, Expression value)
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
                    return Expression.Call(typeof(Serializer<>).MakeGenericType(itemType).GetMethod("SerializeCollection", BindingFlags.NonPublic | BindingFlags.Static), endpoint, value);
                }

                var nullableType = Nullable.GetUnderlyingType(value.Type);
                if (nullableType != null && Check(nullableType).CanSerialize())
                {
                    return Expression.Condition(
                        Expression.MakeMemberAccess(value, value.Type.GetProperty("HasValue")),
                        SerializeExpression(endpoint, Expression.MakeMemberAccess(value, value.Type.GetProperty("Value"))),
                        Expression.Constant(string.Empty));
                }

                if (Check(value.Type).CanBuildDispatcher())
                {
                    return Expression.Call(
                        typeof (TinyProtocol).GetMethod("SerializeRemotableInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                        endpoint,
                        value);
                }

                throw new Exception("cannot serialize expression of type " + value.Type);
            }

            private static Expression DeserializeExpression(Expression endpoint, Expression value, Type targetType)
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
                    var enumerable = Expression.Call(typeof(Serializer<>).MakeGenericType(itemType).GetMethod("DeserializeCollection", BindingFlags.NonPublic | BindingFlags.Static), endpoint, value);
                    if (targetType.IsArray)
                    {
                        return Expression.Call(typeof(Serializer<>).MakeGenericType(itemType).GetMethod("CollectionToArray", BindingFlags.NonPublic | BindingFlags.Static), enumerable);
                    }
                    return enumerable;
                }

                var nullableType = Nullable.GetUnderlyingType(targetType);
                if (nullableType != null && Check(nullableType).CanSerialize())
                {
                    return Expression.Condition(
                        Expression.Call(typeof(string).GetMethod("IsNullOrEmpty"), value),
                        Expression.New(targetType),
                        Expression.New(targetType.GetConstructor(new[] {nullableType}), DeserializeExpression(endpoint, value, nullableType)));
                }

                if (Check(targetType).CanBuildProxy())
                {
                    MethodInfo customFactory;
                    TryGetCustomProxyFactory(targetType, out customFactory);

                    Expression createExpression;
                    if (customFactory != null)
                    {
                        var realProxyType = ProxyBuilder.BuildProxyType(customFactory.GetParameters()[0].ParameterType);
                        createExpression = Expression.Call(
                            customFactory,
                            Expression.New(realProxyType.GetConstructor(new [] { typeof(IEndpoint), typeof(string) }), endpoint, value));
                    }
                    else
                    {
                        var type = ProxyBuilder.BuildProxyType(targetType);
                        createExpression = Expression.New(type.GetConstructor(new[] { typeof(IEndpoint), typeof (string) }), endpoint, value);
                    }

                    return Expression.Condition(
                        Expression.Call(
                            typeof (string).GetMethod("IsNullOrEmpty"),
                            value),
                        Expression.Constant(null, targetType),
                        Expression.Convert(createExpression, targetType));
                }

                if (targetType.IsClass)
                {
                    if (targetType.IsDefined(typeof(DataContractAttribute), true))
                    {
                        return Expression.Call(typeof(Serializer<T>).GetMethod("DeserializeClass", BindingFlags.NonPublic | BindingFlags.Static), value);
                    }
                    return Expression.Block(
                            Expression.Throw(Expression.New(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }), Expression.Constant("cannot deserialize non-data class"))),
                            Expression.Default(targetType));
                }

                throw new Exception("cannot deserialize expression to type " + targetType);
            }

            private static string SerializeCollection(IEndpoint endpoint, IEnumerable<T> collection)
            {
                if (collection == null)
                {
                    return string.Empty;
                }

                return "[" + string.Join(",", collection.Select(x => Instance._serialize(endpoint, x))) + "]";
            }

            private static IList<T> DeserializeCollection(IEndpoint endpoint, string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return value.Substring(1, value.Length - 2).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Instance._deserialize(endpoint, x)).ToList();
            }

            private static T[] CollectionToArray(IEnumerable<T> collection)
            {
                if (collection == null)
                {
                    return null;
                }

                return collection.ToArray();
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
