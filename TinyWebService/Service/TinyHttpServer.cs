using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TinyWebService.Protocol;

namespace TinyWebService.Service
{
    internal sealed class TinyHttpServer : IDisposable
    {
        internal const int DefaultPort = 14048;

        internal static string CreatePrefix(string hostname, int port, string name)
        {
            return string.Format("http://{0}:{1}/{2}/", hostname ?? "localhost", port, name);
        }

        private const string SessionCookieName = "TINYSESSION";

        private readonly string _rootPath;
        private readonly string _metadataPath;
        private readonly Func<Session> _sessionFactory;
        private readonly HttpListener _listener;
        private bool _isDisposed;

        private readonly IDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>(StringComparer.OrdinalIgnoreCase);

        public TinyHttpServer(string name, int port, Func<Session> sessionFactory, bool allowExternalConnections = false)
        {
            _rootPath = "/" + name + "/";
            _metadataPath = _rootPath + "_metadata";
            _sessionFactory = sessionFactory;
            _listener = new HttpListener();
            _listener.Prefixes.Add(CreatePrefix(allowExternalConnections ? "*" : null, port, name));
            _listener.Start();

            Run();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _listener.Stop();
        }

        private async void Run()
        {
            while (!_isDisposed)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException)
                {
                    continue;
                }

                string response;
                try
                {
                    response = HandleRequest(context);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    response = ex.ToString();
                }

                var responseBuffer = Encoding.UTF8.GetBytes(response);
                context.Response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                context.Response.OutputStream.Close();
            }
        }

        private string HandleRequest(HttpListenerContext context)
        {
            if (context.Request.Url.AbsolutePath == _metadataPath)
            {
                return "<meta/>";
            }

            Session session;
            var sessionCookie = context.Request.Cookies[SessionCookieName];
            if (sessionCookie == null || !_sessions.TryGetValue(sessionCookie.Value, out session))
            {
                var sessionId = Guid.NewGuid().ToString("N");
                _sessions[sessionId] = session = _sessionFactory();
                context.Response.Cookies.Add(new Cookie(SessionCookieName, sessionId, _rootPath));
            }

            var index = context.Request.Url.AbsolutePath.IndexOf("/", 1);
            var path = context.Request.Url.AbsolutePath.Substring(index + 1);
            var parameters = ParseQuery(context.Request.Url.Query);
            string instanceId;
            parameters.TryGetValue(TinyProtocol.InstanceIdParameterName, out instanceId);

            return session.Execute(instanceId, path, parameters);
        }

        private static IDictionary<string, string> ParseQuery(string query)
        {
            return query
                .TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x[1]);
        }
    }
}
