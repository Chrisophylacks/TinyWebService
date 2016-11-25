using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TinyWebService.Protocol;
using TinyWebService.Service;
using TinyWebService.Utilities;

namespace TinyWebService.Client
{
    internal sealed class Executor : IExecutor
    {
        private static TinyHttpServer _callbackServer;

        private static string _callbackEndpoint;
        private static Session _callbackSession;

        private readonly string _prefix;

        public Executor(string prefix)
        {
            _prefix = prefix;
            Timeout = TimeSpan.FromSeconds(30);
        }

        public string GetExternalAddress(string path)
        {
            return _prefix + path;
        }

        public string RegisterCallbackInstance(ISimpleDispatcher dispatcher)
        {
            if (_callbackEndpoint == null)
            {
                throw new InvalidOperationException("Duplex communication has not been set up");
            }

            return new CallbackObjectAddress(_callbackEndpoint, _callbackSession.RegisterInstance(dispatcher)).Encode();
        }

        public TimeSpan Timeout { get; set; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void EnableDuplexMode(TinyServiceOptions options)
        {
            if (_callbackEndpoint != null)
            {
                throw new InvalidOperationException("duplex mode already enabled");
            }

            _callbackSession = new Session(new SimpleTimer(options.CleanupInterval));
            _callbackEndpoint = TinyProtocol.CreateEndpoint(options.AllowExternalConnections ? "*" : null, options.Port);
            _callbackServer = new TinyHttpServer(TinyProtocol.CreatePrefixFromEndpoint(_callbackEndpoint), _callbackSession);
        }

        public async Task<string> Execute(string path, IDictionary<string, string> parameters = null)
        {
            try
            {
                using (var response = await ExecuteInternal(path, parameters))
                {
                    return ReadStream(response.GetResponseStream());
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw new TinyWebServiceException("Remote host not found");
                }

                throw new TinyWebServiceException("Remote error: " + ReadStream(ex.Response.GetResponseStream()), ex);
            }
        }

        private async Task<WebResponse> ExecuteInternal(string path, IDictionary<string, string> parameters = null)
        {
            var request = WebRequest.CreateHttp(new Uri(_prefix + path));
            request.Timeout = (int)Timeout.TotalMilliseconds;

            if (parameters == null || parameters.Count == 0)
            {
                request.Method = WebRequestMethods.Http.Get;
            }
            else
            {
                request.Method = WebRequestMethods.Http.Post;
                var content = new FormUrlEncodedContent(parameters);
                await content.CopyToAsync(request.GetRequestStream());
            }

            return await request.GetResponseAsync();
        }

        private string ReadStream(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }
    }
}