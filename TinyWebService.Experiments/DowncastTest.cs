using System;

namespace TinyWebService.Experiments
{
    static class DowncastTest
    {
        public static void Run()
        {
            using (TinyService.Host(new Service()).AtEndpoint("test"))
            {
                var client = TinyClient.Create<IService>("test");
                var @base = client.GetBase();
                @base.Call();
                var specific = TinyClient.CastProxy<IBase, ISpecific>(@base);
                specific.Call();
                specific.CallSpecific();
            }
        }

        internal interface IService
        {
            IBase GetBase();
        }

        class Service : IService
        {
            public IBase GetBase()
            {
                return new Specific();
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