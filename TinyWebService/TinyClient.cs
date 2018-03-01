using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;

namespace TinyWebService
{
    public static class TinyClient
    {
        private static IEndpoint _defaultClientEndpoint;

        internal static IEndpoint DefaultClientEndpoint
        {
            get
            {
                if (_defaultClientEndpoint == null)
                {
                    InitDefaultEndpoint();
                }
                return _defaultClientEndpoint;
            }
        }

        public static T Create<T>(string name, int port = TinyProtocol.DefaultPort, string hostname = null)
            where T : class
        {
            return Get<T>(name)
                .WithPort(port)
                .WithHost(hostname)
                .Connect();
        }

        public static void InitDefaultEndpoint(
            TimeSpan? executionTimeout = null,
            TimeSpan? cleanupInterval = null,
            int callbackPort = TinyProtocol.DefaultCallbackPort)
        {
            _defaultClientEndpoint = new Endpoint(
                TinyProtocol.CreatePrefix(null, TinyProtocol.DefaultCallbackPort, Guid.NewGuid().ToString("N")),
                null,
                false,
                cleanupInterval ?? TimeSpan.Zero,
                executionTimeout ?? TimeSpan.FromSeconds(30));
        }

        public static Builder<T> Get<T>(string name)
            where T : class 
        {
            return new Builder<T>(name);
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

        /// <summary>
        /// Unitialized dispatcher for given proxy. If root proxy is supplied, all nested dispatchers are uninitialized instead.
        /// </summary>
        /// <param name="proxy">proxy object to uninitialize</param>
        /// <returns></returns>
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

        public sealed class Builder<T>
            where T : class
        {
            private readonly string _name;
            private string _hostname;
            private int _port;

            public Builder(string name)
            {
                _name = name;
            }

            public Builder<T> WithHost(string hostname)
            {
                _hostname = hostname;
                return this;
            }

            public Builder<T> WithPort(int port)
            {
                _port = port;
                return this;
            }

            public T Connect(TimeSpan? timeout = null)
            {
                try
                {
                    return ConnectAsync(timeout).Result;
                }
                catch (AggregateException ex)
                {
                    throw ex.GetBaseException();
                }
            }

            public Task<T> ConnectAsync(TimeSpan? timeout = null)
            {
                var actualConnectTimeout = timeout ?? TimeSpan.FromSeconds(1);

                var address = new ObjectAddress(TinyProtocol.CreatePrefix(_hostname, _port, _name), null).Encode();

                var executor = DefaultClientEndpoint.GetExecutor(address, actualConnectTimeout);

                return executor.Execute(TinyProtocol.MetadataPath).ContinueWith(meta =>
                {
                    // use result to correctly rethrow task exceptions, no actual checking yet
                    CheckMeta(meta.Result);

                    return TinyProtocol.Serializer<T>.Deserialize(DefaultClientEndpoint, address);
                }, TaskContinuationOptions.ExecuteSynchronously);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            private static void CheckMeta(string meta)
            {
            }
        }
    }
}
