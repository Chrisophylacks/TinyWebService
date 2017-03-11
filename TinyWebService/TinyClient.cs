using System;
using System.Threading.Tasks;
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
            return TinyProtocol.GetRealProxy(proxy)?.Address.Encode();
        }

        public static TResult CastProxy<TResult>(object proxy)
            where TResult : class
        {
            var realProxy = TinyProtocol.GetRealProxy(proxy);
            return realProxy?.CastProxy<TResult>().Result;
        }

        public static Task KeepAlive(object proxy)
        {
            return TinyProtocol.GetRealProxy(proxy).KeepAlive();
        }

        public static Task Dispose(object proxy)
        {
            return TinyProtocol.GetRealProxy(proxy).DisposeRemote();
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
