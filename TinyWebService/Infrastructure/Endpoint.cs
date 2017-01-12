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
        private Session _session;

        private static IEndpoint _clientEndpoint;

        public static IEndpoint DefaultClientEndpoint
        {
            get { return _clientEndpoint ?? (_clientEndpoint = new Endpoint(TinyProtocol.CreatePrefix(null, TinyServiceOptions.DefaultCallbackPort, Guid.NewGuid().ToString("N")), null, false)); }
        }

        public static IEndpoint CreateServerEndpoint(string prefix, object rootInstance, bool useThreadDispatcher)
        {
            return new Endpoint(prefix, rootInstance, useThreadDispatcher);
        }

        public Endpoint(string prefix, object rootInstance, bool useThreadDispatcher)
        {
            _prefix = prefix;
            _useThreadDispatcher = useThreadDispatcher;
            if (rootInstance != null)
            {
                _session = new Session(new SimpleTimer(TimeSpan.FromMinutes(5)), DispatcherFactory.CreateDispatcher(rootInstance, this, useThreadDispatcher));
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
                _session = new Session(new SimpleTimer(TimeSpan.FromMinutes(5)));
                _server = new TinyHttpServer(_prefix, _session);
            }

            return new ObjectAddress(_prefix, _session.RegisterInstance(DispatcherFactory.CreateDispatcher(instance, this, _useThreadDispatcher))).Encode();
        }

        public void Dispose()
        {
            if (_server != null)
            {
                _server.Dispose();
            }

            if (_session != null)
            {
                _session.Dispose();
            }
        }
   }
}