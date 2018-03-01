using System;
using System.Threading.Tasks;

namespace TinyWebService.Utilities
{
    public static class Tasks
    {
        public static Task<T> FromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task<T> FromException<T>(Exception ex)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(ex);
            return tcs.Task;
        }

        public static T WaitEx<T>(Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }
        }

        public static void WaitEx(Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }
        }
    }
}
