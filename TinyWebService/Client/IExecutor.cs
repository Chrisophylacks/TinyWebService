using System.Collections.Generic;
using System.Threading.Tasks;

namespace TinyWebService.Client
{
    internal interface IExecutor
    {
        string GetExternalAddress(string path);

        Task<string> Execute(string pathAndQuery, IDictionary<string, string> parameters = null);
    }
}