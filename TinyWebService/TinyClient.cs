using System;
using TinyWebService.Service;

namespace TinyWebService
{
    using Client;

    public static class TinyClient
    {
        public static T Create<T>(string name, int port = TinyHttpServer.DefaultPort, string hostname = null)
            where T : class
        {
            var executor = new Executor(TinyHttpServer.CreatePrefix(hostname, port, name)) { Timeout = 1000 };
            var meta = executor.Execute("_metadata");
            executor.Timeout = 30000;
            return ProxyBuilder.CreateProxy<T>(executor);
        }

        public static void RegisterCustomProxyFactory<TProxyFactory>()
            where TProxyFactory : class
        {
            ProxyBuilder.RegisterCustomProxyFactory<TProxyFactory>();
        }
    }
}
