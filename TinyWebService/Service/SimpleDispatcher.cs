using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using TinyWebService.Protocol;
using TinyWebService.Reflection;

namespace TinyWebService.Service
{
    internal sealed class SimpleDispatcher<T> : ISimpleDispatcher
        where T : class
    {
        private static readonly ConcurrentDictionary<string, Func<T, IDictionary<string, string>, Task<object>>> Actions = new ConcurrentDictionary<string, Func<T, IDictionary<string, string>, Task<object>>>();

        private readonly T _instance;
        private Dispatcher _dispatcher;

        public SimpleDispatcher(T instance)
        {
            _instance = instance;
            var dispatcherObject = instance as DispatcherObject;
            if (dispatcherObject != null)
            {
                _dispatcher = dispatcherObject.Dispatcher;
            }
        }

        public Task<object> Execute(string path, IDictionary<string, string> parameters)
        {
            if (_dispatcher == null)
            {
                return ExecuteInternal(path, parameters);
            }

            var tcs = new TaskCompletionSource<object>();
            _dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    var obj = await ExecuteInternal(path, parameters);
                    (obj as ISimpleDispatcher)?.SetDispatcherIfNotPresent(_dispatcher);
                    tcs.SetResult(obj);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }

        public void SetDispatcherIfNotPresent(Dispatcher dispatcher)
        {
            if (_dispatcher == null)
            {
                _dispatcher = dispatcher;
            }
        }

        public void Dispose()
        {
            var disposableInstance = _instance as IDisposable;
            if (disposableInstance != null)
            {
                if (_dispatcher == null)
                {
                    disposableInstance.Dispose();
                }
                else
                {
                    _dispatcher.BeginInvoke(new Action(() => disposableInstance.Dispose()));
                }
            }
        }

        private Task<object> ExecuteInternal(string path, IDictionary<string, string> parameters)
        {
            return Actions.GetOrAdd(path, BindAction)(_instance, parameters);
        }

        private Func<T, IDictionary<string, string>, Task<object>> BindAction(string path)
        {
            var target = Expression.Parameter(typeof (T));
            var parameters = Expression.Parameter(typeof (IDictionary<string, string>));

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

                var indexer = typeof (IDictionary<string, string>).GetProperty("Item");
                var method = current.Type.GetTypeHierarchy().Select(x => x.GetMethod(entries[i], BindingFlags.Public | BindingFlags.Instance)).FirstOrDefault(x => x != null);
                if (method == null)
                {
                    current = ThrowExpression(string.Format("member '{0}' not found", entries[i]));
                    break;
                }

                current = Expression.Call(
                    current,
                    method,
                    method.GetParameters().Select(x => DeserializeParameter(Expression.Property(parameters, indexer, Expression.Constant(x.Name)), x.ParameterType)).ToArray());
            }

            var signature = new AsyncTypeSignature(current.Type);
            if (TinyProtocol.IsSerializableType(signature.ReturnType))
            {
                if (signature.ReturnType == typeof (void))
                {
                    if (signature.IsAsync)
                    {
                        current = Expression.Call(typeof (DispatcherHelpers).GetMethod("WrapTask"), current);
                    }
                    else
                    {
                        current = Expression.Block(current, Expression.Call(typeof (Task).GetMethod("FromResult").MakeGenericMethod(typeof (object)), Expression.Constant(string.Empty)));
                    }
                }
                else
                {
                    current = Expression.Call(typeof (DispatcherHelpers).GetMethod(signature.IsAsync ? "WrapValueAsync" : "WrapValue").MakeGenericMethod(signature.ReturnType), current);
                }
            }
            else if (TinyProtocol.IsRemotableType(signature.ReturnType))
            {
                current = Expression.Call(typeof(DispatcherHelpers).GetMethod(signature.IsAsync ? "WrapInstanceAsync" : "WrapInstance").MakeGenericMethod(signature.ReturnType), current);
            }
            else
            {
                throw new Exception("cannot invoke method - unsupported return type");
            }

            return Expression.Lambda<Func<T, IDictionary<string, string>, Task<object>>>(
                current,
                target,
                parameters).Compile();
        }

        private Expression ThrowExpression(string message)
        {
            return Expression.Block(
                Expression.Throw(
                    Expression.New(typeof (InvalidOperationException).GetConstructor(new[] { typeof (string) }), Expression.Constant(message))),
                Expression.Constant(string.Empty));
        }

        private Expression DeserializeParameter(Expression parameter, Type parameterType)
        {
            if (TinyProtocol.IsSerializableType(parameterType))
            {
                return parameter.Deserialize(parameterType);
            }

            if (TinyProtocol.IsRemotableType(parameterType))
            {
                return Expression.Call(typeof (TinyClient).GetMethod("CreateProxyFromAddress", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(parameterType), parameter);
            }

            throw new InvalidOperationException(string.Format("Unsupported parameter type '{0}'", parameterType));
        }
    }
}