using System;
using System.Net;
using System.Text;
using System.Web;

namespace TinyWebService.Service
{
    internal sealed class TinyHttpServer : IDisposable
    {
        private readonly Session _session;

        internal static string CreatePrefix(string hostname, int port, string name)
        {
            return string.Format("http://{0}:{1}/{2}/", hostname ?? "localhost", port, name);
        }

        private readonly HttpListener _listener;
        private bool _isDisposed;

        public TinyHttpServer(string prefix, Session session)
        {
            _session = session;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
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
                try
                {
                    HandleRespose(await _listener.GetContextAsync().ConfigureAwait(false));
                }
                catch (HttpListenerException)
                {
                }
            }
        }

        private async void HandleRespose(HttpListenerContext context)
        {
            string response;
            try
            {
                response = await _session.Execute(HttpUtility.UrlDecode(context.Request.Url.AbsolutePath), HttpUtility.UrlDecode(context.Request.Url.Query));
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
}
