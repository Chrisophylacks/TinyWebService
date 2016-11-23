using System;
using System.Runtime.ExceptionServices;
using TinyWebService.Protocol;

namespace TinyWebService
{
    using Client;

    public static class TinyClient
    {
        public static T Create<T>(string name, int port = TinyServiceOptions.DefaultPort, string hostname = null)
            where T : class
        {
            var executor = new Executor(TinyProtocol.CreatePrefix(hostname, port, name)) { Timeout = TimeSpan.FromSeconds(1) };

            try
            {
                executor.Execute(TinyProtocol.MetadataPath).Wait();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            executor.Timeout = TimeSpan.FromSeconds(30);
            return ProxyBuilder.CreateProxy<T>(executor);
        }

        public static void EnableDuplexMode(TinyServiceOptions options = null)
        {
            Executor.EnableDuplexMode(options ?? new TinyServiceOptions { Port = TinyServiceOptions.DefaultCallbackPort });
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

        public static string GetExternalAddress<T>(T proxyObject)
        {
            var proxy = proxyObject as ProxyBase;
            return proxy?.GetExternalAddress();
        }

        internal static T CreateCallbackProxy<T>(string encodedAddress)
            where T : class
        {
            var address = CallbackObjectAddress.Parse(encodedAddress);
            var executor = new Executor(TinyProtocol.CreatePrefixFromEndpoint(address.Endpoint));
            return ProxyBuilder.CreateProxy<T>(executor, address.InstanceId);
        }
    }
}
