using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebService.Client
{
    internal sealed class Executor : IExecutor
    {
        private static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding(28591);

        private readonly string _prefix;

        public Executor(string prefix, TimeSpan executionTimeout)
        {
            _prefix = prefix;
            Timeout = executionTimeout;
        }

        public TimeSpan Timeout { get; set; }

        public Task<string> Execute(string path, IDictionary<string, string> parameters = null)
        {
            return ExecuteInternal(path, parameters).ContinueWith<string>(x =>
            {
                try
                {
                    using (var response = x.Result)
                    {
                        return ReadStream(response.GetResponseStream());
                    }
                }
                catch (Exception ex)
                {
                    HandleExecutionException(ex);
                    throw;
                }
            });
        }

        private void HandleExecutionException(Exception ex)
        {
            var webException = ex as WebException ?? (ex.GetBaseException() as WebException);
            if (webException != null)
            {
                if (webException.Status == WebExceptionStatus.Timeout)
                {
                    throw new TimeoutException();
                }

                if (webException.Response == null)
                {
                    throw new TinyWebServiceException("Remote host not found");
                }

                throw new TinyWebServiceException("Remote error: " + ReadStream(webException.Response.GetResponseStream()), ex);
            }
        }

        private Task<WebResponse> ExecuteInternal(string path, IDictionary<string, string> parameters = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(_prefix + path));
            request.Timeout = (int)Timeout.TotalMilliseconds;

            if (parameters == null || parameters.Count == 0)
            {
                request.Method = WebRequestMethods.Http.Get;
            }
            else
            {
                request.Method = WebRequestMethods.Http.Post;
                var content = GetContentByteArray(parameters);
                request.GetRequestStream().Write(content, 0, content.Length);
            }

            return Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null, TaskCreationOptions.None);
        }

        private string ReadStream(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        {
            if (nameValueCollection == null)
                throw new ArgumentNullException("nameValueCollection");
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> keyValuePair in nameValueCollection)
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append('&');
                stringBuilder.Append(Encode(keyValuePair.Key));
                stringBuilder.Append('=');
                stringBuilder.Append(Encode(keyValuePair.Value));
            }
            return DefaultHttpEncoding.GetBytes(stringBuilder.ToString());
        }

        private static string Encode(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;
            return Uri.EscapeDataString(data).Replace("%20", "+");
        }
    }
}