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
        private readonly TimeSpan _executionTimeout;
        private Session _session;

        public Endpoint(string prefix, object rootInstance, bool useThreadDispatcher, TimeSpan cleanupInterval, TimeSpan executionTimeout)
        {
            _prefix = prefix;
            _useThreadDispatcher = useThreadDispatcher;
            _cleanupInterval = cleanupInterval;
            _tickInterval = TimeSpan.FromMinutes(1);
            _executionTimeout = executionTimeout;
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

        public IExecutor GetExecutor(string prefix, TimeSpan? overrideTimeout = null)
        {
            return new Executor(prefix, overrideTimeout ?? _executionTimeout);
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