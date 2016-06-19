using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Threading;
using TinyWebService.Protocol;

namespace TinyWebService.Service
{
    internal sealed class SimpleDispatcher<T> : ISimpleDispatcher
        where T : class
    {
        private static readonly ConcurrentDictionary<string, Func<T, IDictionary<string, string>, object>> Actions = new ConcurrentDictionary<string, Func<T, IDictionary<string, string>, object>>();

        private readonly T _instance;
        private readonly Dispatcher _dispatcher;

        public SimpleDispatcher(T instance)
        {
            _instance = instance;
            var dispatcherObject = instance as DispatcherObject;
            if (dispatcherObject != null)
            {
                _dispatcher = dispatcherObject.Dispatcher;
            }
        }

        public object Execute(string path, IDictionary<string, string> parameters)
        {
            if (_dispatcher == null)
            {
                return ExecuteInternal(path, parameters);
            }

            return _dispatcher.Invoke(() => ExecuteInternal(path, parameters));
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

        private object ExecuteInternal(string path, IDictionary<string, string> parameters)
        {
            return Actions.GetOrAdd(path, BindAction)(_instance, parameters);
        }

        private Func<T, IDictionary<string, string>, object> BindAction(string path)
        {
            var target = Expression.Parameter(typeof (T));
            var parameters = Expression.Parameter(typeof (IDictionary<string, string>));

            Expression current = target;
            var entries = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; ++i)
            {
                var property = current.Type.GetProperty(entries[i], BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    current = Expression.Property(current, property);
                    continue;
                }

                if (i != entries.Length - 1)
                {
                    throw new Exception("member not found");
                }

                var indexer = typeof (IDictionary<string, string>).GetProperty("Item");
                var method = current.Type.GetMethod(entries[i], BindingFlags.Public | BindingFlags.Instance);
                current = Expression.Call(
                    current,
                    method,
                    method.GetParameters().Select(x => Expression.Property(parameters, indexer, Expression.Constant(x.Name)).Deserialize(x.ParameterType)).ToArray());
            }

            if (TinyProtocol.IsSerializableType(current.Type))
            {
                current = current.Serialize();
            }
            else if (TinyProtocol.IsRemotableType(current.Type))
            {
                current = Expression.New(typeof (SimpleDispatcher<>).MakeGenericType(current.Type).GetConstructor(new[] { current.Type }), current);
            }
            else
            {
                throw new Exception("cannot invoke method - unsupported return type");
            }

            return Expression.Lambda<Func<T, IDictionary<string, string>, object>>(
                Expression.Convert(current, typeof(object)),
                target,
                parameters).Compile();
        }
    }
}