using System.Threading.Tasks;

namespace TinyWebService.Client
{
    internal interface IExecutor
    {
        Task<string> Execute(string pathAndQuery);
    }
}