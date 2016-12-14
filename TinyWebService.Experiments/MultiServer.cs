using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebService.Experiments
{
    public interface IMultiService
    {
        IMultiService Get(int depth);

        void Print(string message);
    }

    public class MultiService : IMultiService
    {
        private readonly int _depth;
        private IMultiService _next;
        private int _index;

        public static IMultiService Create(int depth)
        {
            var endpoint = "Multi_" + depth;
            TinyService.Host(new MultiService(depth)).AtEndpoint(endpoint);
            return TinyClient.Create<IMultiService>(endpoint);
        }

        private MultiService(int depth)
        {
            _depth = depth;
        }

        public IMultiService Get(int depth)
        {
            if (depth == _depth)
            {
                return this;
            }

            if (_next == null)
            {
                _next = Create(_depth + 1);
            }

            return _next.Get(depth);
        }

        public void Print(string message)
        {
            Console.WriteLine("[{0}] {1}", _depth, message);
        }
    }

    public static class MultiServiceTest
    {
        public static void Run()
        {
            var service = MultiService.Create(0);
            var s3 = service.Get(3);

            service.Print("message 0");
            s3.Print("message 3");
        }
    }
}
