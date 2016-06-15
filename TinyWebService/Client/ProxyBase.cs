using System;
using System.Text;
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

        protected string ExecuteQuery(string subPath, StringBuilder query)
        {
            return _executor.Execute(_pathPrefix + subPath + query);
        }

        protected T CreateMemberProxy<T>(string subPath)
            where T : class
        {
            return ProxyBuilder.CreateProxy<T>(_executor, _instanceId, _pathPrefix + subPath);
        }

        protected T CreateDetachedProxy<T>(string subPath, StringBuilder query)
            where T : class
        {
            var instanceId = ExecuteQuery(subPath, query);
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
