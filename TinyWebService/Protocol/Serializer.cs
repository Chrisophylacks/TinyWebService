using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TinyWebService.Protocol
{
    internal static class Serializer<T>
    {
        static Serializer()
        {
            var value = Expression.Parameter(typeof (T));
            Serialize = Expression.Lambda<Func<T, string>>(value.Serialize(), value).Compile();

            value = Expression.Parameter(typeof (string));
            Deserialize = Expression.Lambda<Func<string, T>>(value.Deserialize(typeof(T)), value).Compile();
        }

        public static Func<T, string> Serialize { get; private set; }
        public static Func<string, T> Deserialize { get; private set; }

        public static string SerializeCollection(IEnumerable<T> collection)
        {
            return "[" + string.Join(",", collection.Select(Serializer<T>.Serialize)) + "]";
        }

        public static IEnumerable<T> DeserializeCollection(string value)
        {
            return value.Substring(1, value.Length - 2).Split(',').Select(Deserialize).ToList();
        }
    }
}
