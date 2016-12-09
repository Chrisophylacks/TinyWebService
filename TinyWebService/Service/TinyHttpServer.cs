using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TinyWebService.Service
{
    internal sealed class TinyHttpServer : IDisposable
    {
        private readonly Session _session;

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

        private void Run()
        {
            if (!_isDisposed)
            {
                Task.Factory.FromAsync<HttpListenerContext>(_listener.BeginGetContext, _listener.EndGetContext, null, TaskCreationOptions.None).ContinueWith(x =>
                {
                    try
                    {
                        HandleRespose(x.Result);
                    }
                    catch (HttpListenerException)
                    {
                    }

                    Run();
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void HandleRespose(HttpListenerContext context)
        {
            var path = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            var query = context.Request.HttpMethod == "GET" ? context.Request.Url.Query : new StreamReader(context.Request.InputStream).ReadToEnd();

            _session.Execute(path, HttpUtility.UrlDecode(query)).ContinueWith(x =>
            {
                string response;
                try
                {
                    response = x.Result;
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    response = ex.ToString();
                }

                var responseBuffer = Encoding.UTF8.GetBytes(response ?? String.Empty);
                context.Response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                context.Response.OutputStream.Close();
            }, TaskContinuationOptions.ExecuteSynchronously);

        }
    }
}
