using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TinyWebService.Service
{
    internal interface ISimpleDispatcher : IDisposable
    {
        Task<object> Execute(string path, IDictionary<string, string> parameters);

        void SetDispatcherIfNotPresent(Dispatcher dispatcher);
    }
}