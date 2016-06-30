using System.Threading.Tasks;

namespace TinyWebService.Tests.Stubs
{
    public interface IMethodRoot
    {
        int GetIntValue();
        Task<int> GetIntValueAsync();

        void Invoke();
        Task InvokeAsync();

        IMethodRoot Clone();
        Task<IMethodRoot> CloneAsync();
    }
}