using System;
using TinyWebService.Protocol;
using TinyWebService.Service;
using TinyWebService.Utilities;

namespace TinyWebService
{
    public static class TinyService
    {
        public static Builder<T> Host<T>(T service)
            where T : class
        {
            return new Builder<T>(service);
        }

        public sealed class Builder<T>
            where T : class
        {
            private readonly T _service;
            private bool _useThreadDispatcher;
            private bool _allowExternalConnections;
            private int _port = 14048;
            private TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

            internal Builder(T service)
            {
                _service = service;
            }

            public Builder<T> WithCurrentThreadDispatcher()
            {
                _useThreadDispatcher = true;
                return this;
            }

            public Builder<T> WithPort(int port)
            {
                _port = port;
                return this;
            }

            public Builder<T> WithCleanupInterval(TimeSpan interval)
            {
                _cleanupInterval = interval;
                return this;
            }

            public Builder<T> AllowExternalConnections(bool allow = true)
            {
                _allowExternalConnections = allow;
                return this;
            }

            public IDisposable AtEndpoint(string endpointName)
            {
                var prefix = TinyProtocol.CreatePrefix(_allowExternalConnections ? "*" : null, _port, endpointName);
                var session = new Session(prefix, new SimpleTimer(_cleanupInterval), new SimpleDispatcher<T>(_service, _useThreadDispatcher));
                return new TinyHttpServer(prefix, session);
            }
        }
    }
}