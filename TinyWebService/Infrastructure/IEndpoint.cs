using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyWebService.Client;
using TinyWebService.Protocol;

namespace TinyWebService.Infrastructure
{
    internal interface IEndpoint : IDisposable
    {
        string RegisterInstance(object instance);
        IExecutor GetExecutor(string prefix);
    }
}
