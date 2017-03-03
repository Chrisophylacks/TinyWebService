using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyWebService.Experiments
{
    interface INullableTestService
    {
        double? Double(double? value);
    }

    class NullableTestService : INullableTestService
    {
        public double? Double(double? value) => value * 2;
    }

    class NullableTest
    {
        public static void Run()
        {
            using (TinyService.Host(new NullableTestService()).AtEndpoint("test"))
            {
                var client = TinyClient.Create<INullableTestService>("test");
                Console.WriteLine("null -> " + client.Double(null));
                Console.WriteLine("100 -> " + client.Double(100));
            }
        }
    }
}
