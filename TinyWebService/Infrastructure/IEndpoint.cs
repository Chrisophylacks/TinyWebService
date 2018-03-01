using System;
using TinyWebService.Client;

namespace TinyWebService.Infrastructure
{
    internal interface IEndpoint : IDisposable
    {
        string RegisterInstance(object instance);
        IExecutor GetExecutor(string prefix, TimeSpan? overrideTimeout = null);
    }
}
