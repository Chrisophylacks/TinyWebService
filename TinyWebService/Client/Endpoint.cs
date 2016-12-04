using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TinyWebService.Protocol;
using TinyWebService.Service;
using TinyWebService.Utilities;

namespace TinyWebService.Client
{
    internal static class Endpoint
    {
        private static TinyHttpServer _callbackServer;
        private static readonly ConcurrentDictionary<string, IExecutor> Executors = new ConcurrentDictionary<string, IExecutor>();

        private static string _callbackPrefix;
        private static Session _callbackSession;
            
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void EnableDuplexMode(TinyServiceOptions options)
        {
            if (_callbackPrefix != null)
            {
                throw new InvalidOperationException("duplex mode already enabled");
            }

            _callbackPrefix = TinyProtocol.CreatePrefix(options.AllowExternalConnections ? "*" : null, options.Port, Guid.NewGuid().ToString("N"));
            _callbackSession = new Session(_callbackPrefix, new SimpleTimer(options.CleanupInterval));
            _callbackServer = new TinyHttpServer(_callbackPrefix, _callbackSession);
        }

        public static string RegisterCallbackInstance(ISimpleDispatcher dispatcher)
        {
            if (_callbackPrefix == null)
            {
                throw new InvalidOperationException("Duplex communication has not been set up");
            }

            return new ObjectAddress(_callbackPrefix, _callbackSession.RegisterInstance(dispatcher)).Encode();
        }

        public static IExecutor GetExecutor(string prefix)
        {
            return Executors.GetOrAdd(prefix, x => new Executor(x));
        }
    }
}