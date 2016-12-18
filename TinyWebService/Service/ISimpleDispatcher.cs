using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TinyWebService.Service
{
    internal interface ISimpleDispatcher : IDisposable
    {
        Task<string> Execute(string path, IDictionary<string, string> parameters);
    }
}