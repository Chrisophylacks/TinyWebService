using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace TinyWebService.Reflection
{
    internal sealed class ExpressionPropertyBuilder
    {
        private readonly PropertyBuilder _property;
        private readonly MethodBuilder _getterImpl;
        private readonly MethodBuilder _setterImpl;

        public ExpressionPropertyBuilder(TypeBuilder typeBuilder, PropertyInfo property)
        {
            _property = typeBuilder.DefineProperty(property.Name, property.Attributes, property.PropertyType, Type.EmptyTypes);
            if (property.CanRead)
            {
                _getterImpl = typeBuilder.DefineMethod("<>getimpl_" + property.Name, MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, property.PropertyType, new[] { typeof(object) });
                var getter = property.GetGetMethod();
                var methodBuilder = typeBuilder.DefineMethod("get_" + property.Name, getter.Attributes & ~MethodAttributes.Abstract, getter.CallingConvention, getter.ReturnType, Type.EmptyTypes);
                methodBuilder.GetILGenerator().EmitMethodStub(_getterImpl, 0);
                _property.SetGetMethod(methodBuilder);
            }

            if (property.CanWrite)
            {
                _setterImpl = typeBuilder.DefineMethod("<>setimpl_" + property.Name, MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new[] { typeof(object), property.PropertyType });
                var getter = property.GetGetMethod();
                var methodBuilder = typeBuilder.DefineMethod("set_" + property.Name, getter.Attributes & ~MethodAttributes.Abstract, getter.CallingConvention, typeof (void), new[] { property.PropertyType });
                methodBuilder.GetILGenerator().EmitMethodStub(_setterImpl, 1);
                _property.SetSetMethod(methodBuilder);
            }

            This = Expression.Parameter(typeof(object), "this");
            Value = Expression.Parameter(property.PropertyType, "value");
        }

        public PropertyInfo Property
        { 
            get { return _property; }
        }

        public ParameterExpression Value { get; private set; }

        public ParameterExpression This { get; private set; }

        public void ImplementGetter(Expression body)
        {
            Expression.Lambda(body, This).CompileToMethod(_getterImpl);
        }

        public void ImplementSetter(Expression body)
        {
            Expression.Lambda(body, This, Value).CompileToMethod(_setterImpl);
        }
    }
}