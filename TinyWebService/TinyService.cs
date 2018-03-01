using System;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;

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
            private int _port = TinyProtocol.DefaultPort;
            private TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
            private TimeSpan _executionTimeout = TimeSpan.FromSeconds(30);

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

            public Builder<T> WithExecutionTimeout(TimeSpan timeout)
            {
                _executionTimeout = timeout;
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
                return new Endpoint(prefix, _service, _useThreadDispatcher, _cleanupInterval, _executionTimeout);
            }
        }
    }
}