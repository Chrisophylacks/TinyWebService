using System;

namespace TinyWebService.Experiments
{
    static class OverloadsTest
    {
        public static void Run()
        {
            using (TinyService.Host(new Service()).AtEndpoint("test"))
            {
                var client = TinyClient.Create<IService>("test");
                Console.WriteLine(client.Call("1"));
                Console.WriteLine(client.Call(1));
                Console.WriteLine(client.Call(1.0));
            }
        }

        internal interface IServiceBase
        {
            string Call(string value);
        }

        internal interface IService : IServiceBase
        {
            string Call(int value);
            string Call(double value);
        }

        class Service : IService
        {
            public string Call(string value)
            {
                return "S:" + value;
            }

            public string Call(int value)
            {
                return "I:" + value;
            }

            public string Call(double value)
            {
                return "D:" + value;
            }
        }

        internal interface IBase
        {
            void Call();
        }

        internal interface ISpecific : IBase
        {
            void CallSpecific();
        }

        class Specific : ISpecific
        {
            public void Call()
            {
                Console.WriteLine("Call");
            }

            public void CallSpecific()
            {
                Console.WriteLine("CallSpecific");
            }
        }
    }
}