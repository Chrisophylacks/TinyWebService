using System.Threading.Tasks;

namespace TinyWebService.Tests.Stubs
{
    public class MethodRoot : IMethodRoot
    {
        public int GetIntValue()
        {
            return 1;
        }

        public Task<int> GetIntValueAsync()
        {
            return Task.FromResult(1);
        }

        public void Invoke()
        {
        }

        public Task InvokeAsync()
        {
            return Task.FromResult<object>(null);
        }

        public IMethodRoot Clone()
        {
            return new DispatcherRoot();
        }

        public Task<IMethodRoot> CloneAsync()
        {
            return Task.FromResult<IMethodRoot>(new DispatcherRoot());
        }
    }
}