using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TinyWebService.Client
{
    internal interface IExecutor
    {
        Task<string> Execute(string pathAndQuery, IDictionary<string, string> parameters = null);

        TimeSpan Timeout { get; set; }
    }
}