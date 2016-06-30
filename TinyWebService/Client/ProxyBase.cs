using System.Text;
using System.Threading.Tasks;
using TinyWebService.Protocol;

namespace TinyWebService.Client
{
    internal abstract class ProxyBase
    {
        private readonly string _pathPrefix;
        private readonly IExecutor _executor;
        private readonly string _instanceId;

        protected ProxyBase(IExecutor executor, string instanceId = null, string path = null)
        {
            _pathPrefix = string.IsNullOrEmpty(path) ? string.Empty : path + "/";
            _executor = executor;
            _instanceId = instanceId;
        }

        protected Task ExecuteQuery(string subPath, StringBuilder query)
        {
            return _executor.Execute(_pathPrefix + subPath + query);
        }

        protected async Task<T> ExecuteQueryRet<T>(string subPath, StringBuilder query)
        {
            return Serializer<T>.Deserialize(await _executor.Execute(_pathPrefix + subPath + query));
        }

        protected T CreateMemberProxy<T>(string subPath)
            where T : class
        {
            return ProxyBuilder.CreateProxy<T>(_executor, _instanceId, _pathPrefix + subPath);
        }

        protected async Task<T> CreateDetachedProxy<T>(string subPath, StringBuilder query)
            where T : class
        {
            var instanceId = await _executor.Execute(_pathPrefix + subPath + query);
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            return ProxyBuilder.CreateProxy<T>(_executor, instanceId);
        }

        protected StringBuilder CreateQueryBuilder()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_instanceId))
            {
                AppendParameter(sb, TinyProtocol.InstanceIdParameterName, _instanceId);
            }
            return sb;
        }

        protected void AppendParameter(StringBuilder sb, string parameterName, string parameterValue)
        {
            sb.Append(sb.Length == 0 ? '?' : '&');
            sb.Append(parameterName);
            sb.Append('=');
            sb.Append(parameterValue);
        }
    }
}
