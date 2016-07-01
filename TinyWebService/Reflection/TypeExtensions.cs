using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TinyWebService.Reflection
{
    internal static class TypeExtensions
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        public static PropertyInfo FindPublicProperty(this Type type, string name)
        {
            return type.GetTypeHierarchy().Select(x => x.GetProperty(name, Flags)).FirstOrDefault(x => x != null);
        }

        public static MethodInfo FindPublicMethod(this Type type, string name)
        {
            return type.GetTypeHierarchy().Select(x => x.GetMethod(name, Flags)).FirstOrDefault(x => x != null);
        }

        public static IEnumerable<PropertyInfo> GetPublicProperies(this Type type)
        {
            return type.GetTypeHierarchy().SelectMany(x => x.GetProperties(Flags));
        }

        public static IEnumerable<MethodInfo> GetPublicMethods(this Type type)
        {
            return type.GetTypeHierarchy().SelectMany(x => x.GetMethods(Flags));
        }

        public static IEnumerable<Type> GetTypeHierarchy(this Type type)
        {
            return type.GetInterfaces().Concat(new[] { type });
        }

        public static Type TryGetCollectionItemType(this Type type)
        {
            if (type.IsGenericType && typeof (IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                return type.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
