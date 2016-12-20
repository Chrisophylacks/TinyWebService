using System;

namespace TinyWebService.Experiments
{
    static class DowncastTest
    {
        public static void Run()
        {
            var service = new ValueContainer<IService> { Value = new Service() };
            using (TinyService.Host(service).AtEndpoint("test"))
            {
                var client = TinyClient.Create<IValueContainer<IService>>("test");
                var base1 = client.Value.GetNewBase();
                base1.Call();
                var specific1 = TinyClient.CastProxy<ISpecific>(base1);
                specific1.Call();
                specific1.CallSpecific();

                var base2 = client.Value.Base;
                base2.Call();
                var specific2 = TinyClient.CastProxy<ISpecific>(base2);
                specific2.Call();
                specific2.CallSpecific();
            }
        }

        internal interface IService
        {
            IBase Base { get; }
            IBase GetNewBase();
        }

        class Service : IService
        {
            public IBase Base { get; } = new Specific();

            public IBase GetNewBase()
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