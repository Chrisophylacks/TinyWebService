using System;
using TinyWebService.Client;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;

namespace TinyWebService
{
    public static class TinyClient
    {
        public static T Create<T>(string name, int port = TinyServiceOptions.DefaultPort, string hostname = null)
            where T : class
        {
            var address = new ObjectAddress(TinyProtocol.CreatePrefix(hostname, port, name), null).Encode();

            var executor = Endpoint.DefaultClientEndpoint.GetExecutor(address);
            executor.Timeout = TimeSpan.FromSeconds(1);

            try
            {
                executor.Execute(TinyProtocol.MetadataPath).Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
                //ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            return TinyProtocol.Serializer<T>.Deserialize(Endpoint.DefaultClientEndpoint, address);
        }

        public static string GetExternalAddress(object proxy)
        {
            var realProxy = TinyProtocol.GetRealProxy(proxy);
            if (realProxy == null)
            {
                return null;
            }

            return realProxy.Address.Encode();
        }

        public static TResult CastProxy<TResult>(object proxy)
            where TResult : class
        {
            var realProxy = TinyProtocol.GetRealProxy(proxy);
            if (realProxy == null)
            {
                return null;
            }

            return realProxy.CastProxy<TResult>().Result;
        }

        public static void RegisterCustomProxyFactory<TProxyFactory>()
            where TProxyFactory : class
        {
            TinyProtocol.RegisterCustomProxyFactory<TProxyFactory>();
        }

        public static void RegisterCustomSerializer<TValue>(Func<TValue, string> serialize, Func<string, TValue> deserialize)
        {
            TinyProtocol.Serializer<TValue>.RegisterCustom(serialize, deserialize);
        }
    }
}
