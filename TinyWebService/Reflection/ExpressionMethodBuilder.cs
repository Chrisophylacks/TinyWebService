using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace TinyWebService.Reflection
{
    internal sealed class ExpressionMethodBuilder
    {
        private readonly MethodBuilder _methodImpl;

        internal ExpressionMethodBuilder(TypeBuilder typeBuilder, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
            _methodImpl = typeBuilder.DefineMethod("<>impl_" + method.Name, MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, method.ReturnType, new[] { typeof(object) }.Concat(parameterTypes).ToArray());
            var methodBuilder = typeBuilder.DefineMethod(method.Name, method.Attributes & ~MethodAttributes.Abstract, method.CallingConvention, method.ReturnType, parameterTypes);
            if (method.IsGenericMethod)
            {
                methodBuilder.DefineGenericParameters(method.GetGenericArguments().Select(x => x.Name).ToArray());
            }

            methodBuilder.GetILGenerator().EmitMethodStub(_methodImpl, parameterTypes.Length);
            Method = methodBuilder;

            This = Expression.Parameter(typeof(object), "@this");
            Parameters = parameters.Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();
        }

        public MethodInfo Method { get; private set; }

        public ParameterExpression This { get; }

        public ParameterExpression[] Parameters { get; }

        public void Implement(Expression body)
        {
            Expression.Lambda(body, new[] { This }.Concat(Parameters)).CompileToMethod(_methodImpl);
        }
    }
}