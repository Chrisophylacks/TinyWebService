using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;
using TinyWebService.Reflection;

namespace TinyWebService.Client
{
    internal static class ProxyBuilder
    {
        private static readonly AssemblyBuilder ProxiesAssembly;
        private static readonly ModuleBuilder ProxiesModule;

        private static readonly MethodInfo ExecuteQuery;
        private static readonly MethodInfo ExecuteQueryRet;
        private static readonly MethodInfo CreateQuery;
        private static readonly MethodInfo CreateMemberProxy;
        private static readonly MethodInfo AddParameter;

        private static readonly ConstructorInfo ExceptionConstructor;

        private static int _counter;

        static ProxyBuilder()
        {
            ProxiesAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("TinyWebService.Proxies"), AssemblyBuilderAccess.RunAndSave);
            ProxiesModule = ProxiesAssembly.DefineDynamicModule("TinyWebService.Proxies.dll");

            ExecuteQuery = typeof (ProxyBase).GetMethod("ExecuteQuery", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ExecuteQueryRet = typeof(ProxyBase).GetMethod("ExecuteQueryRet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CreateQuery = typeof(ProxyBase).GetMethod("CreateQueryBuilder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CreateMemberProxy = typeof(ProxyBase).GetMethod("CreateMemberProxy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            AddParameter = typeof (IDictionary<string, string>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

            ExceptionConstructor = typeof (InvalidOperationException).GetConstructor(new[] { typeof (string) });
        }

        public static void Dump()
        {
            ProxiesAssembly.Save("TinyWebService.Proxies.dll");
        }

        public static Type BuildProxyType(Type interfaceType)
        {
            var typeBuilder = ProxiesModule.DefineType(interfaceType.Name + "_Proxy" + _counter++, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, typeof(ProxyBase), new[] { interfaceType });
            var constructorParameterTypes = new[] { typeof (IEndpoint), typeof (string) };
            var baseConstructor = typeof (ProxyBase).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, constructorParameterTypes, null);

            var constructorBuilder = typeBuilder.DefineExpressionConstructor(baseConstructor);
            var constructorExpressions = new List<Expression>();

            foreach (var method in interfaceType.GetPublicMethods())
            {
                if (method.IsSpecialName || !method.IsVirtual)
                {
                    continue;
                }

                if (method.IsGenericMethod)
                {
                    var parameters = method.GetParameters();
                    var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
                    var methodBuilder = typeBuilder.DefineMethod(method.Name, method.Attributes & ~MethodAttributes.Abstract, method.CallingConvention, method.ReturnType, parameterTypes);
                    methodBuilder.DefineGenericParameters(method.GetGenericArguments().Select(x => x.Name).ToArray());
                    var il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldstr, "cannot invoke generic method");
                    il.Emit(OpCodes.Newobj, ExceptionConstructor);
                    il.Emit(OpCodes.Throw);
                    if (method.ReturnType != typeof (void))
                    {
                        il.Emit(OpCodes.Ldloc, il.DeclareLocal(method.ReturnType));
                    }
                    il.Emit(OpCodes.Ret);

                    continue;
                }

                var signature = new AsyncTypeSignature(method.ReturnType);
                var builder = typeBuilder.DefineExpressionMethod(method);

                var dict = Expression.Parameter(typeof(IDictionary<string, string>));
                var block = new List<Expression>();
                block.Add(Expression.Assign(dict, builder.This.CallMember(CreateQuery)));

                string errorMessage = null;
                if (builder.Parameters.All(x => TinyProtocol.Check(x.Type).CanSerialize()))
                {
                    block.AddRange(builder.Parameters.Select(x => dict.CallMember(
                        AddParameter,
                        Expression.Constant(x.Name),
                        x.Serialize(builder.This.MemberField(typeof(ProxyBase).GetField("Endpoint", BindingFlags.Instance | BindingFlags.NonPublic))))));

                    if (TinyProtocol.Check(signature.ReturnType).CanDeserialize())
                    {
                        var call = signature.ReturnType == typeof (void)
                            ? builder.This.CallMember(ExecuteQuery, Expression.Constant(method.Name), dict)
                            : builder.This.CallMember(ExecuteQueryRet.MakeGenericMethod(signature.ReturnType), Expression.Constant(method.Name), dict);
                        block.Add(signature.IsAsync ? call : Wait(call));
                    }
                    else
                    {
                        errorMessage = string.Format("incompatible return type '{0}'", signature.ReturnType);
                    }
                }
                else
                {
                    errorMessage = "cannot serialize all parameter types";
                }

                if (errorMessage != null)
                {
                    block.Add(Expression.Throw(Expression.New(ExceptionConstructor, Expression.Constant(errorMessage))));
                    block.Add(Expression.Default(method.ReturnType));
                }

                builder.Implement(Expression.Block(
                    method.ReturnType,
                    new[] { dict },
                    block));
            }

            foreach (var property in interfaceType.GetPublicProperies())
            {
                var builder = typeBuilder.DefineExpressionProperty(property);
                if (property.CanRead)
                {
                    ImplementGetter(typeBuilder, builder, property);
                }

                if (property.CanWrite)
                {
                    ImplementSetter(builder, property);
                }
            }

            constructorExpressions.Add(Expression.Empty());
            constructorBuilder.Implement(Expression.Block(typeof(void), constructorExpressions));
            return typeBuilder.CreateType();
        }

        private static void ImplementGetter(TypeBuilder typeBuilder, ExpressionPropertyBuilder builder, PropertyInfo property)
        {
            if (TinyProtocol.Check(property.PropertyType).CanBuildProxy())
            {
                var field = typeBuilder.DefineField("<prx>_" + property.Name, property.PropertyType, FieldAttributes.Private);
                builder.ImplementGetter(
                    Expression.Coalesce(
                        builder.This.MemberField(field),
                        Expression.Assign(
                            builder.This.MemberField(field),
                            builder.This.CallMember(CreateMemberProxy.MakeGenericMethod(property.PropertyType), Expression.Constant(property.Name)))));
                return;
            }

            bool isSerializable = TinyProtocol.Check(property.PropertyType).CanDeserialize();
            var dict = Expression.Parameter(typeof(IDictionary<string, string>));
            var block = new List<Expression>();
            block.Add(Expression.Assign(dict, builder.This.CallMember(CreateQuery)));
            if (isSerializable)
            {
                block.Add(Wait(builder.This.CallMember(ExecuteQueryRet.MakeGenericMethod(property.PropertyType), Expression.Constant(property.Name), dict)));
            }
            else
            {
                block.Add(Expression.Throw(Expression.New(typeof(Exception).GetConstructor(new [] { typeof(string) }), Expression.Constant("incompatible return type"))));
                block.Add(Expression.Default(property.PropertyType));
            }

            builder.ImplementGetter(
                Expression.Block(
                    property.PropertyType,
                    new[] { dict },
                    block));
        }

        private static void ImplementSetter(ExpressionPropertyBuilder builder, PropertyInfo property)
        {
            var dict = Expression.Parameter(typeof(IDictionary<string, string>));
            var block = new List<Expression>();
            block.Add(Expression.Assign(dict, builder.This.CallMember(CreateQuery)));
            block.Add(dict.CallMember(AddParameter, Expression.Constant("value"), builder.Value.Serialize(builder.This.MemberField(typeof(ProxyBase).GetField("Endpoint", BindingFlags.Instance | BindingFlags.NonPublic)))));
            if (TinyProtocol.Check(property.PropertyType).CanSerialize())
            {
                block.Add(Wait(builder.This.CallMember(ExecuteQuery, Expression.Constant(property.Name), dict)));
            }
            else
            {
                block.Add(Expression.Throw(Expression.New(typeof(Exception).GetConstructor(new[] { typeof(string) }), Expression.Constant("incompatible return type"))));
                block.Add(Expression.Default(property.PropertyType));
            }

            builder.ImplementSetter(
                Expression.Block(
                    typeof(void),
                    new[] { dict },
                    block));
        }

        private static Expression Wait(Expression taskExpression)
        {
            if (taskExpression.Type.IsGenericType)
            {
                return Expression.Property(taskExpression, taskExpression.Type.GetProperty("Result"));
            }

            return Expression.Call(taskExpression, taskExpression.Type.GetMethod("Wait", Type.EmptyTypes));
        }
    }
}