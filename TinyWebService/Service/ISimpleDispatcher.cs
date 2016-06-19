using System;
using System.Collections.Generic;

namespace TinyWebService.Service
{
    internal interface ISimpleDispatcher : IDisposable
    {
        object Execute(string path, IDictionary<string, string> parameters);
    }
}