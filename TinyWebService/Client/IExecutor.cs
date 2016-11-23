using System.Collections.Generic;
using System.Threading.Tasks;
using TinyWebService.Service;

namespace TinyWebService.Client
{
    internal interface IExecutor
    {
        string GetExternalAddress(string path);

        string RegisterCallbackInstance(ISimpleDispatcher dispatcher);

        Task<string> Execute(string pathAndQuery, IDictionary<string, string> parameters = null);
    }
}