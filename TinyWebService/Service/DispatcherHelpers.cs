using System.Threading.Tasks;
using TinyWebService.Client;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;
using TinyWebService.Utilities;

namespace TinyWebService.Service
{
    internal sealed class DispatcherHelpers
    {
        public static Task<string> WrapTask(Task task)
        {
            return task.ContinueWith(x =>
            {
                if (x.Exception != null)
                {
                    throw x.Exception;
                }
                return string.Empty;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task<string> WrapValue<TValue>(IEndpoint endpoint, TValue value)
        {
            return Tasks.FromResult(TinyProtocol.Serializer<TValue>.Serialize(endpoint, value));
        }

        public static Task<string> WrapValueAsync<TValue>(IEndpoint endpoint, Task<TValue> task)
        {
            return task.ContinueWith(x => TinyProtocol.Serializer<TValue>.Serialize(endpoint, x.Result), TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}