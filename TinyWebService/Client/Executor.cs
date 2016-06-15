using System;
using System.IO;
using System.Net;

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

        public string Execute(string pathAndQuery)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(_prefix + pathAndQuery));
            request.CookieContainer = _cookies;
            request.Timeout = Timeout;

            using (var response = request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }
    }
}