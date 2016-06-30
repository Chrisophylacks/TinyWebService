using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TinyWebService.Tests.Stubs
{
    public class DispatcherRoot : DispatcherObject, IMethodRoot
    {
        public int GetIntValue()
        {
            VerifyAccess();
            return 1;
        }

        public Task<int> GetIntValueAsync()
        {
            VerifyAccess();
            return Task.FromResult(1);
        }

        public void Invoke()
        {
            VerifyAccess();
        }

        public Task InvokeAsync()
        {
            VerifyAccess();
            return Task.FromResult<object>(null);
        }

        public IMethodRoot Clone()
        {
            VerifyAccess();
            return new DispatcherRoot();
        }

        public Task<IMethodRoot> CloneAsync()
        {
            VerifyAccess();
            return Task.FromResult<IMethodRoot>(new DispatcherRoot());
        }
    }
}
