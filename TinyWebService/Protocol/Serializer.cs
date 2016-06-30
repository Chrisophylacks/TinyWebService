using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebService.Protocol
{
    internal static class Serializer<T>
    {
        private static Func<T, string> _serialize;
        private static Func<string, T> _deserialize;

        static Serializer()
        {
            var value = Expression.Parameter(typeof (T));
            _serialize = Expression.Lambda<Func<T, string>>(value.Serialize(), value).Compile();

            value = Expression.Parameter(typeof (string));
            _deserialize = Expression.Lambda<Func<string, T>>(value.Deserialize(typeof(T)), value).Compile();
        }

        public static string Serialize(T instance)
        {
            return _serialize(instance);
        }

        public static T Deserialize(string instance)
        {
            return _deserialize(instance);
        }
    }
}
