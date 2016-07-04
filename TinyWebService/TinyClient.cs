using System;
using System.Runtime.ExceptionServices;
using TinyWebService.Protocol;
using TinyWebService.Service;

namespace TinyWebService
{
    using Client;

    public static class TinyClient
    {
        public static T Create<T>(string name, int port = TinyServiceOptions.DefaultPort, string hostname = null)
            where T : class
        {
            var executor = new Executor(TinyHttpServer.CreatePrefix(hostname, port, name)) { Timeout = 1000 };

            try
            {
                executor.Execute(TinyProtocol.MetadataPath).Wait();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            executor.Timeout = 30000;
            return ProxyBuilder.CreateProxy<T>(executor);
        }

        public static void RegisterCustomProxyFactory<TProxyFactory>()
            where TProxyFactory : class
        {
            ProxyBuilder.RegisterCustomProxyFactory<TProxyFactory>();
        }

        public static void RegisterCustomSerializer<TValue>(Func<TValue, string> serialize, Func<string, TValue> deserialize)
        {
            TinyProtocol.Serializer<TValue>.RegisterCustom(serialize, deserialize);
        }
    }
}
