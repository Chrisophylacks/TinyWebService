using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TinyWebService.Client
{
    internal sealed class Executor : IExecutor
    {
        private readonly string _prefix;
        private readonly CookieContainer _cookies = new CookieContainer();

        public Executor(string prefix)
        {
            _prefix = prefix;
            Timeout = 30000;
        }

        public int Timeout { get; set; }

        public async Task<string> Execute(string pathAndQuery)
        {
            var request = (HttpWebRequest) WebRequest.Create(new Uri(_prefix + pathAndQuery));
            request.CookieContainer = _cookies;
            request.Timeout = Timeout;

            try
            {
                using (var response = await request.GetResponseAsync())
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

                throw new TinyWebServiceException("Remote error: " + ReadStream(ex.Response.GetResponseStream()) , ex);
            }
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