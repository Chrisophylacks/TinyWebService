using System.Threading.Tasks;
using TinyWebService.Client;
using TinyWebService.Protocol;
using TinyWebService.Utilities;

namespace TinyWebService.Service
{
    internal sealed class DispatcherHelpers
    {
        public static Task<object> WrapTask(Task task)
        {
            return task.ContinueWith<object>(x =>
            {
                if (x.Exception != null)
                {
                    throw x.Exception;
                }
                return string.Empty;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task<object> WrapValue<TValue>(TValue value)
        {
            return Tasks.FromResult<object>(TinyProtocol.Serializer<TValue>.Serialize(value));
        }

        public static Task<object> WrapValueAsync<TValue>(Task<TValue> task)
        {
            return task.ContinueWith<object>(x => TinyProtocol.Serializer<TValue>.Serialize(x.Result), TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task<object> WrapInstance<TInstance>(TInstance instance)
            where TInstance : class
        {
            if (instance == null)
            {
                return Tasks.FromResult<object>(null);
            }

            var proxy = instance as ProxyBase;
            if (proxy != null)
            {
                return Tasks.FromResult<object>(proxy.GetExternalAddress());
            }

            return Tasks.FromResult<object>(new SimpleDispatcher<TInstance>(instance));
        }

        public static Task<object> WrapInstanceAsync<TInstance>(Task<TInstance> task)
            where TInstance : class
        {
            return task.ContinueWith<object>(x =>
            {
                var instance = x.Result;
                if (instance == null)
                {
                    return null;
                }

                var proxy = instance as ProxyBase;
                if (proxy != null)
                {
                    return proxy.GetExternalAddress();
                }

                return new SimpleDispatcher<TInstance>(instance);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}