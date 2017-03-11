using System;
using TinyWebService.Client;
using TinyWebService.Protocol;
using TinyWebService.Service;
using TinyWebService.Utilities;

namespace TinyWebService.Infrastructure
{
    internal sealed class Endpoint : IEndpoint
    {
        private TinyHttpServer _server;

        private readonly string _prefix;
        private readonly bool _useThreadDispatcher;
        private readonly TimeSpan _cleanupInterval;
        private readonly TimeSpan _tickInterval;
        private Session _session;

        private static IEndpoint _clientEndpoint;

        public static IEndpoint DefaultClientEndpoint => _clientEndpoint ?? (_clientEndpoint = new Endpoint(TinyProtocol.CreatePrefix(null, TinyServiceOptions.DefaultCallbackPort, Guid.NewGuid().ToString("N")), null, false, TimeSpan.Zero));

        public static IEndpoint CreateServerEndpoint(string prefix, object rootInstance, bool useThreadDispatcher, TimeSpan cleanupInterval)
        {
            return new Endpoint(prefix, rootInstance, useThreadDispatcher, cleanupInterval);
        }

        public Endpoint(string prefix, object rootInstance, bool useThreadDispatcher, TimeSpan cleanupInterval)
        {
            _prefix = prefix;
            _useThreadDispatcher = useThreadDispatcher;
            _cleanupInterval = cleanupInterval;
            _tickInterval = TimeSpan.FromMinutes(1);
            if (_tickInterval > _cleanupInterval)
            {
                _tickInterval = _cleanupInterval;
            }

            if (rootInstance != null)
            {
                _session = new Session(new SimpleTimer(_tickInterval), _cleanupInterval, DispatcherFactory.CreateDispatcher(rootInstance, this, useThreadDispatcher));
                _server = new TinyHttpServer(prefix, _session);
            }
        }

        public IExecutor GetExecutor(string prefix)
        {
            return new Executor(prefix);
        }

        public string RegisterInstance(object instance)
        {
            if (_session == null)
            {
                _session = new Session(new SimpleTimer(_tickInterval), _cleanupInterval);
                _server = new TinyHttpServer(_prefix, _session);
            }

            return new ObjectAddress(_prefix, _session.RegisterInstance(DispatcherFactory.CreateDispatcher(instance, this, _useThreadDispatcher))).Encode();
        }

        public void Dispose()
        {
            _server?.Dispose();
            _session?.Dispose();
        }
   }
}