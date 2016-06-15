using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace TinyWebService.Reflection
{
    internal static class TypeBuilderExtensions
    {
        private static readonly Func<MethodInfo, Expression, IList<Expression>, Expression> _createCallMemberExpression;
        private static readonly Func<Expression, FieldInfo, Expression> _createMemberFieldExpression;

        static TypeBuilderExtensions()
        {
            var type = typeof(Expression).Assembly.GetType("System.Linq.Expressions.InstanceMethodCallExpressionN");
            var constr = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(MethodInfo), typeof(Expression), typeof(IList<Expression>) }, null);
            var parameters = new[]
            {
                Expression.Parameter(typeof (MethodInfo)),
                Expression.Parameter(typeof (Expression)),
                Expression.Parameter(typeof (IList<Expression>))
            };
            _createCallMemberExpression = Expression.Lambda<Func<MethodInfo, Expression, IList<Expression>, Expression>>(Expression.New(constr, parameters), parameters).Compile();

            type = typeof(Expression).Assembly.GetType("System.Linq.Expressions.FieldExpression");
            constr = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Expression), typeof(FieldInfo) }, null);
            parameters = new[]
            {
                Expression.Parameter(typeof (Expression)),
                Expression.Parameter(typeof (FieldInfo)),
            };
            _createMemberFieldExpression = Expression.Lambda<Func<Expression, FieldInfo, Expression>>(Expression.New(constr, parameters), parameters).Compile();
        }

        public static Expression CallMember(this ParameterExpression thisExpression, MethodInfo method, params Expression[] parameters)
        {
            return _createCallMemberExpression(method, Expression.Convert(thisExpression, method.DeclaringType), parameters.ToList());
        }

        public static Expression MemberField(this ParameterExpression thisExpression, FieldInfo field)
        {
            return _createMemberFieldExpression(Expression.Convert(thisExpression, field.DeclaringType), field);
        }

        public static ExpressionConstructorBuilder DefineExpressionConstructor(this TypeBuilder typeBuilder, ConstructorInfo baseConstructor)
        {
            return new ExpressionConstructorBuilder(typeBuilder, baseConstructor);
        }

        public static ExpressionMethodBuilder DefineExpressionMethod(this TypeBuilder typeBuilder, MethodInfo method)
        {
            return new ExpressionMethodBuilder(typeBuilder, method);
        }

        public static ExpressionPropertyBuilder DefineExpressionProperty(this TypeBuilder typeBuilder, PropertyInfo property)
        {
            return new ExpressionPropertyBuilder(typeBuilder, property);
        }

        public static void EmitConstructorStub(this ConstructorBuilder constructorBuilder, ConstructorInfo baseConstructor, bool ret)
        {
            var il = constructorBuilder.GetILGenerator();
            var parametersCount = baseConstructor.GetParameters().Length;
            il.Emit(OpCodes.Ldarg_0);
            for (int i = 1; i <= parametersCount; ++i)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Call, baseConstructor);
            if (ret)
            {
                il.Emit(OpCodes.Ret);
            }
        }

        public static void EmitMethodStub(this ILGenerator il, MethodInfo targetMethod, int parametersCount)
        {
            il.Emit(OpCodes.Ldarg_0);
            for (int i = 1; i <= parametersCount; ++i)
            {
                il.Emit(OpCodes.Ldarg_S, (byte)i);
            }
            il.Emit(OpCodes.Call, targetMethod);
            il.Emit(OpCodes.Ret);
        }
    }
}
