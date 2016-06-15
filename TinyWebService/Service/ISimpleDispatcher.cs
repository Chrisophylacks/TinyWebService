using System.Collections.Generic;

namespace TinyWebService.Service
{
    internal interface ISimpleDispatcher
    {
        object Execute(string path, IDictionary<string, string> parameters);
    }
}