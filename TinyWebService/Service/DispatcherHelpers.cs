using System.Threading.Tasks;
using TinyWebService.Client;
using TinyWebService.Protocol;

namespace TinyWebService.Service
{
    internal sealed class DispatcherHelpers
    {
        public static async Task<object> WrapTask(Task task)
        {
            await task.ConfigureAwait(false);
            return string.Empty;
        }

        public static Task<object> WrapValue<TValue>(TValue value)
        {
            return Task.FromResult<object>(TinyProtocol.Serializer<TValue>.Serialize(value));
        }

        public static async Task<object> WrapValueAsync<TValue>(Task<TValue> task)
        {
            return TinyProtocol.Serializer<TValue>.Serialize(await task.ConfigureAwait(false));
        }

        public static Task<object> WrapInstance<TInstance>(TInstance instance)
            where TInstance : class
        {
            if (instance == null)
            {
                return Task.FromResult<object>(null);
            }

            var proxy = instance as ProxyBase;
            if (proxy != null)
            {
                return Task.FromResult<object>(proxy.GetExternalAddress());
            }

            return Task.FromResult<object>(new SimpleDispatcher<TInstance>(instance));
        }

        public static async Task<object> WrapInstanceAsync<TInstance>(Task<TInstance> task)
            where TInstance : class
        {
            var instance = await task.ConfigureAwait(false);
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
        }
    }
}