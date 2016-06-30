using System.Threading.Tasks;
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
            return Task.FromResult<object>(Serializer<TValue>.Serialize(value));
        }

        public static async Task<object> WrapValueAsync<TValue>(Task<TValue> task)
        {
            return Serializer<TValue>.Serialize(await task.ConfigureAwait(false));
        }

        public static Task<object> WrapInstance<TInstance>(TInstance instance)
            where TInstance : class
        {
            return Task.FromResult<object>(new SimpleDispatcher<TInstance>(instance));
        }

        public static async Task<object> WrapInstanceAsync<TInstance>(Task<TInstance> task)
            where TInstance : class
        {
            return new SimpleDispatcher<TInstance>(await task.ConfigureAwait(false));
        }
    }
}