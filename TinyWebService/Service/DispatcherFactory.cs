using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;
using TinyWebService.Reflection;
using TinyWebService.Utilities;

namespace TinyWebService.Service
{
    internal sealed class DispatcherFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<object, IEndpoint, ISimpleDispatcher>> dispatcherFactories = new ConcurrentDictionary<Type, Func<object, IEndpoint, ISimpleDispatcher>>();

        public static ISimpleDispatcher CreateDispatcher(object instance, IEndpoint endpoint, bool useThreadDispatcher)
        {
            var innerDispatcher = dispatcherFactories.GetOrAdd(instance.GetType(), GetFactoryForType)(instance, endpoint);

            Dispatcher threadDispatcher = null;
            var dispatcherObject = instance as DispatcherObject;
            if (dispatcherObject != null)
            {
                threadDispatcher = dispatcherObject.Dispatcher;
            }
            else if (useThreadDispatcher)
            {
                threadDispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            }

            if (threadDispatcher == null)
            {
                return innerDispatcher;
            }

            return new SynchronizedDispatcher(innerDispatcher, threadDispatcher);
        }

        private static Func<object, IEndpoint, ISimpleDispatcher> GetFactoryForType(Type type)
        {
            var instance = Expression.Parameter(typeof(object));
            var endpoint = Expression.Parameter(typeof (IEndpoint));
            return Expression.Lambda<Func<object, IEndpoint, ISimpleDispatcher>>(
                Expression.New(typeof (SimpleDispatcher<>).MakeGenericType(type).GetConstructor(new[] { type, typeof(IEndpoint) }), Expression.Convert(instance, type), endpoint),
                instance,
                endpoint).Compile();
        }

        private sealed class SimpleDispatcher<T> : ISimpleDispatcher
            where T : class
        {
            private static readonly ConcurrentDictionary<string, Func<T, IEndpoint, IDictionary<string, string>, Task<string>>> Actions = new ConcurrentDictionary<string, Func<T, IEndpoint, IDictionary<string, string>, Task<string>>>();

            private readonly T _instance;

            private readonly IEndpoint _endpoint;

            public SimpleDispatcher(T instance, IEndpoint endpoint)
            {
                _instance = instance;
                _endpoint = endpoint;
            }

            public Task<string> Execute(string path, IDictionary<string, string> parameters)
            {
                try
                {
                    return Actions.GetOrAdd(path, BindAction)(_instance, _endpoint, parameters);
                }
                catch (Exception ex)
                {
                    return Tasks.FromException<string>(ex);
                }
            }

            public void Dispose()
            {
                var disposable = _instance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            private Func<T, IEndpoint, IDictionary<string, string>, Task<string>> BindAction(string path)
            {
                var target = Expression.Parameter(typeof(T));
                var endpoint = Expression.Parameter(typeof (IEndpoint));
                var parameters = Expression.Parameter(typeof(IDictionary<string, string>));

                Expression current = target;
                var entries = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < entries.Length; ++i)
                {
                    var property = current.Type.GetTypeHierarchy().Select(x => x.GetProperty(entries[i], BindingFlags.Public | BindingFlags.Instance)).FirstOrDefault(x => x != null);
                    if (property != null)
                    {
                        current = Expression.Property(current, property);
                        continue;
                    }

                    if (i != entries.Length - 1)
                    {
                        current = ThrowExpression(string.Format("member '{0}' not found", entries[i]));
                        break;
                    }

                    if (entries[i] == TinyProtocol.DetachCommand)
                    {
                        break;
                    }

                    var indexer = typeof (IDictionary<string, string>).GetProperty("Item");
                    var method = current.Type.GetTypeHierarchy()
                        .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        .Where(x => !x.IsGenericMethod)
                        .FirstOrDefault(x => x.Name == entries[i] && x.GetParameters().All(p => TinyProtocol.Check(p.ParameterType).CanDeserialize()));
                    if (method == null)
                    {
                        current = ThrowExpression(string.Format("member '{0}' not found", entries[i]));
                        break;
                    }

                    current = Expression.Call(
                        current,
                        method,
                        method.GetParameters().Select(x => DeserializeParameter(Expression.Property(parameters, indexer, Expression.Constant(x.Name)), endpoint, x.ParameterType)).ToArray());
                }

                var signature = new AsyncTypeSignature(current.Type);
                if (TinyProtocol.Check(signature.ReturnType).CanSerialize())
                {
                    if (signature.ReturnType == typeof(void))
                    {
                        if (signature.IsAsync)
                        {
                            current = Expression.Call(typeof(DispatcherHelpers).GetMethod("WrapTask"), current);
                        }
                        else
                        {
                            current = Expression.Block(current, Expression.Call(typeof(Task).GetMethod("FromResult").MakeGenericMethod(typeof(string)), Expression.Constant(string.Empty)));
                        }
                    }
                    else
                    {
                        current = Expression.Call(typeof(DispatcherHelpers).GetMethod(signature.IsAsync ? "WrapValueAsync" : "WrapValue").MakeGenericMethod(signature.ReturnType), endpoint, current);
                    }
                }
                else
                {
                    throw new Exception("cannot invoke method - unsupported return type");
                }

                return Expression.Lambda<Func<T, IEndpoint, IDictionary<string, string>, Task<string>>>(
                    current,
                    target,
                    endpoint,
                    parameters).Compile();
            }

            private Expression ThrowExpression(string message)
            {
                return Expression.Block(
                    Expression.Throw(
                        Expression.New(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }), Expression.Constant(message))),
                    Expression.Constant(string.Empty));
            }

            private Expression DeserializeParameter(Expression parameter, Expression endpoint, Type parameterType)
            {
                if (TinyProtocol.Check(parameterType).CanDeserialize())
                {
                    return parameter.Deserialize(endpoint, parameterType);
                }

                throw new InvalidOperationException(string.Format("Unsupported parameter type '{0}'", parameterType));
            }
        }

        private sealed class SynchronizedDispatcher : ISimpleDispatcher
        {
            private readonly ISimpleDispatcher _innerDispatcher;
            private readonly Dispatcher _threadDispatcher;

            public SynchronizedDispatcher(ISimpleDispatcher innerDispatcher, Dispatcher threadDispatcher)
            {
                _innerDispatcher = innerDispatcher;
                _threadDispatcher = threadDispatcher;
            }

            public Task<string> Execute(string path, IDictionary<string, string> parameters)
            {
                var tcs = new TaskCompletionSource<string>();
                _threadDispatcher.BeginInvoke(new Action(() =>
                {
                    _innerDispatcher.Execute(path, parameters).ContinueWith(x =>
                    {
                        try
                        {
                            tcs.SetResult(x.Result);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    });
                }));

                return tcs.Task;
            }

            public void Dispose()
            {
                _threadDispatcher.BeginInvoke(new Action(() => _innerDispatcher.Dispose()));
            }
        }
    }
}