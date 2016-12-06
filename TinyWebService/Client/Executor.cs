using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TinyWebService.Client
{
    internal sealed class Executor : IExecutor
    {
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

        public TimeSpan Timeout { get; set; }

        public async Task<string> Execute(string path, IDictionary<string, string> parameters = null)
        {
            try
            {
                using (var response = await ExecuteInternal(path, parameters).ConfigureAwait(false))
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
                await content.CopyToAsync(request.GetRequestStream()).ConfigureAwait(false);
            }

            return await request.GetResponseAsync().ConfigureAwait(false);
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