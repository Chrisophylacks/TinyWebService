using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
