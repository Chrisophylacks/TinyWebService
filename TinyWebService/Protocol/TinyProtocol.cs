using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace TinyWebService.Protocol
{
    internal static class TinyProtocol
    {
        public const string InstanceIdParameterName = "instanceId";
        public const string MetadataPath = "_metadata";

        public static bool IsSerializableType(Type type)
        {
            return type.IsPrimitive || type == typeof (string) || type == typeof (void);
        }

        public static bool IsRemotableType(Type type)
        {
            return type.IsInterface;
        }

        public static Expression Serialize(this Expression value)
        {
            if (value.Type == typeof (void))
            {
                return Expression.Block(value, Expression.Constant(string.Empty));
            }

            if (value.Type == typeof (string))
            {
                return value;
            }

            if (value.Type.IsPrimitive)
            {
                return Expression.Call(
                    null,
                    typeof (Convert).GetMethod("ToString", BindingFlags.Public | BindingFlags.Static, null, new[] { value.Type, typeof (IFormatProvider) }, null),
                    value,
                    Expression.Property(null, typeof(CultureInfo).GetProperty("InvariantCulture")));
            }

            throw new Exception("cannot serialize expression");
        }

        public static Expression Deserialize(this Expression value, Type targetType)
        {
            if (targetType == typeof(void))
            {
                return Expression.Empty();
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

            throw new Exception("cannot deserialize expression");
        }
    }
}
