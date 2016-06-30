using System;
using System.Reflection;
using System.Threading.Tasks;

namespace TinyWebService.Reflection
{
    internal sealed class AsyncTypeSignature
    {
        public AsyncTypeSignature(Type type)
        {
            if (typeof (Task).IsAssignableFrom(type))
            {
                ReturnType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof (void);
                AsyncType = type;
            }
            else
            {
                ReturnType = type;
            }
        }

        public bool IsAsync => AsyncType != null;

        public Type ReturnType { get; }

        public Type AsyncType { get; }
    }
}