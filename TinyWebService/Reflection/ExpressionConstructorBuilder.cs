using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace TinyWebService.Reflection
{
    internal sealed class ExpressionConstructorBuilder
    {
        private readonly MethodBuilder _methodImpl;

        internal ExpressionConstructorBuilder(TypeBuilder typeBuilder, ConstructorInfo baseConstructor)
        {
            var parameterTypes = baseConstructor.GetParameters().Select(x => x.ParameterType).ToArray();
            _methodImpl = typeBuilder.DefineMethod("<>impl_ctor", MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new[] { typeof(object) }.Concat(parameterTypes).ToArray());
            var constrBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
            constrBuilder.EmitConstructorStub(baseConstructor, false);
            constrBuilder.GetILGenerator().EmitMethodStub(_methodImpl, parameterTypes.Length);

            This = Expression.Parameter(typeof(object), "@this");
            Parameters = parameterTypes.Select(Expression.Parameter).ToArray();
        }

        public ParameterExpression This { get; }

        public ParameterExpression[] Parameters { get; }

        public void Implement(Expression body)
        {
            Expression.Lambda(body, new[] { This }.Concat(Parameters)).CompileToMethod(_methodImpl);
        }
    }
}