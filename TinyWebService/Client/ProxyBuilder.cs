﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using TinyWebService.Protocol;
using TinyWebService.Reflection;

namespace TinyWebService.Client
{
    internal static class ProxyBuilder
    {
        private static readonly Type[] ConstructorParameterTypes = { typeof(IExecutor), typeof(string), typeof(string) };
        private static readonly ConcurrentDictionary<Type, Func<IExecutor, string, string, object>> ProxyFactories = new ConcurrentDictionary<Type, Func<IExecutor, string, string, object>>();
        private static readonly ConcurrentDictionary<Type, MethodInfo> CustomFactories = new ConcurrentDictionary<Type, MethodInfo>();
        private static readonly AssemblyBuilder ProxiesAssembly;
        private static readonly ModuleBuilder ProxiesModule;

        private static readonly MethodInfo BuildProxy;
        private static readonly MethodInfo ExecuteQuery;
        private static readonly MethodInfo CreateQuery;
        private static readonly MethodInfo CreateMemberProxy;
        private static readonly MethodInfo CreateDetachedProxy;
        private static readonly MethodInfo AppendParameter;
        private static readonly MethodInfo HandleGenericMethod;

        private static readonly ConstructorInfo ExceptionConstructor;

        static ProxyBuilder()
        {
            ProxiesAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("TinyWebService.Proxies"), AssemblyBuilderAccess.RunAndSave);
            ProxiesModule = ProxiesAssembly.DefineDynamicModule("TinyWebService.Proxies.dll");

            BuildProxy = typeof(ProxyBuilder).GetMethod("CreateProxy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            ExecuteQuery = typeof (ProxyBase).GetMethod("ExecuteQuery", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CreateQuery = typeof(ProxyBase).GetMethod("CreateQueryBuilder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CreateMemberProxy = typeof(ProxyBase).GetMethod("CreateMemberProxy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CreateDetachedProxy = typeof(ProxyBase).GetMethod("CreateDetachedProxy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            AppendParameter = typeof(ProxyBase).GetMethod("AppendParameter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            HandleGenericMethod = typeof(ProxyBase).GetMethod("HandleGeneric", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            ExceptionConstructor = typeof (InvalidOperationException).GetConstructor(new[] { typeof (string) });
        }

        public static void Dump()
        {
            ProxiesAssembly.Save("TinyWebService.Proxies.dll");
        }

        public static T CreateProxy<T>(IExecutor executor, string instanceId = null, string path = null)
            where T : class
        {
            return (T)ProxyFactories.GetOrAdd(typeof (T), BuildProxyFactory)(executor, instanceId, path);
        }

        public static void RegisterCustomProxyFactory<TProxyFactory>()
            where TProxyFactory : class
        {
            foreach (var method in typeof (TProxyFactory).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                CustomFactories[method.ReturnType.IsGenericType ? method.ReturnType.GetGenericTypeDefinition() : method.ReturnType] = method;
            }
        }

        private static T Throw<T>()
            where T : class
        {
            return CreateProxy<T>(null);
            throw new InvalidOperationException("cannot invoke generic method");
        }

        private static void Throw()
        {
            throw new InvalidOperationException("cannot invoke generic method");
        }

        private static bool CanBuildProxy(Type type)
        {
            return TinyProtocol.IsRemotableType(type)
                   || ProxyFactories.ContainsKey(type)
                   || CustomFactories.ContainsKey(type)
                   || type.IsGenericType && CustomFactories.ContainsKey(type.GetGenericTypeDefinition());
        }

        private static Func<IExecutor, string, string, object> BuildProxyFactory(Type interfaceType)
        {
            MethodInfo customFactory = null;

            if (interfaceType.IsGenericType)
            {
                MethodInfo customFactoryPrototype;
                if (CustomFactories.TryGetValue(interfaceType.GetGenericTypeDefinition(), out customFactoryPrototype))
                {
                    customFactory = customFactoryPrototype.MakeGenericMethod(interfaceType.GetGenericArguments());
                }
            }
            else
            {
                CustomFactories.TryGetValue(interfaceType, out customFactory);
            }

            Expression createExpression;
            var parameters = ConstructorParameterTypes.Select(Expression.Parameter).ToArray();

            if (customFactory != null)
            {
                createExpression = Expression.Call(customFactory, Expression.Call(BuildProxy.MakeGenericMethod(customFactory.GetParameters()[0].ParameterType), parameters));
            }
            else
            {
                var type = BuildProxyType(interfaceType);
                createExpression = Expression.New(type.GetConstructor(ConstructorParameterTypes), parameters);
            }

            return Expression.Lambda<Func<IExecutor, string, string, object>>(
                Expression.Convert(createExpression, typeof(object)),
                parameters).Compile();
        }

        private static Type BuildProxyType(Type interfaceType)
        {
            var typeBuilder = ProxiesModule.DefineType(interfaceType.FullName + "_Proxy", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, typeof(ProxyBase), new[] { interfaceType });
            var baseConstructor = typeof(ProxyBase).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, ConstructorParameterTypes, null);

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

                var builder = typeBuilder.DefineExpressionMethod(method);

                var sb = Expression.Parameter(typeof(StringBuilder));
                var block = new List<Expression>();
                block.Add(Expression.Assign(sb, builder.This.CallMember(CreateQuery)));
                block.AddRange(builder.Parameters.Select(x => builder.This.CallMember(AppendParameter, sb, Expression.Constant(x.Name), x.Serialize())));
                if (CanBuildProxy(method.ReturnType))
                {
                    block.Add(builder.This.CallMember(CreateDetachedProxy.MakeGenericMethod(method.ReturnType), Expression.Constant(method.Name), sb));
                }
                else if (TinyProtocol.IsSerializableType(method.ReturnType))
                {
                    block.Add(builder.This.CallMember(ExecuteQuery, Expression.Constant(method.Name), sb).Deserialize(method.ReturnType));
                }
                else
                {
                    block.Add(Expression.Throw(Expression.New(ExceptionConstructor, Expression.Constant("incompatible return type"))));
                    block.Add(Expression.Default(method.ReturnType));
                }

                builder.Implement(Expression.Block(
                    method.ReturnType,
                    new[] { sb },
                    block));
            }

            foreach (var property in interfaceType.GetPublicProperies())
            {
                var builder = typeBuilder.DefineExpressionProperty(property);
                if (property.CanRead)
                {
                    ImplementGetter(typeBuilder, constructorBuilder, builder, property, constructorExpressions);
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

        private static void ImplementGetter(TypeBuilder typeBuilder, ExpressionConstructorBuilder constructorBuilder, ExpressionPropertyBuilder builder, PropertyInfo property, List<Expression> constructorExpressions)
        {
            if (CanBuildProxy(property.PropertyType))
            {
                var field = typeBuilder.DefineField("<prx>_" + property.Name, property.PropertyType, FieldAttributes.Private);
                constructorExpressions.Add(Expression.Assign(constructorBuilder.This.MemberField(field), constructorBuilder.This.CallMember(CreateMemberProxy.MakeGenericMethod(property.PropertyType), Expression.Constant(property.Name))));
                builder.ImplementGetter(builder.This.MemberField(field));
                return;
            }

            var sb = Expression.Parameter(typeof(StringBuilder));
            var block = new List<Expression>();
            block.Add(Expression.Assign(sb, builder.This.CallMember(CreateQuery)));
            if (TinyProtocol.IsSerializableType(property.PropertyType))
            {
                block.Add(builder.This.CallMember(ExecuteQuery, Expression.Constant(property.Name), sb).Deserialize(property.PropertyType));
            }
            else
            {
                block.Add(Expression.Throw(Expression.New(typeof(Exception).GetConstructor(new [] { typeof(string) }), Expression.Constant("incompatible return type"))));
                block.Add(Expression.Default(property.PropertyType));
            }

            builder.ImplementGetter(
                Expression.Block(
                    property.PropertyType,
                    new[] { sb },
                    block));
        }

        private static void ImplementSetter(ExpressionPropertyBuilder builder, PropertyInfo property)
        {
            var sb = Expression.Parameter(typeof(StringBuilder));
            var block = new List<Expression>();
            block.Add(Expression.Assign(sb, builder.This.CallMember(CreateQuery)));
            block.Add(builder.This.CallMember(AppendParameter, sb, Expression.Constant("value"), builder.Value.Serialize()));
            if (TinyProtocol.IsSerializableType(property.PropertyType))
            {
                block.Add(builder.This.CallMember(ExecuteQuery, Expression.Constant(property.Name), sb).Deserialize(property.PropertyType));
            }
            else
            {
                block.Add(Expression.Throw(Expression.New(typeof(Exception).GetConstructor(new[] { typeof(string) }), Expression.Constant("incompatible return type"))));
                block.Add(Expression.Default(property.PropertyType));
            }

            builder.ImplementSetter(
                Expression.Block(
                    typeof(void),
                    new[] { sb },
                    block));
        }
    }
}