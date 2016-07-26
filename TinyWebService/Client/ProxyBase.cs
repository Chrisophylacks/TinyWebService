﻿using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TinyWebService.Protocol;
using TinyWebService.Service;

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

        protected Task ExecuteQuery(string subPath, IDictionary<string, string> parameters)
        {
            return _executor.Execute(_pathPrefix + subPath, parameters);
        }

        protected async Task<T> ExecuteQueryRet<T>(string subPath, IDictionary<string, string> parameters)
        {
            return TinyProtocol.Serializer<T>.Deserialize(await _executor.Execute(_pathPrefix + subPath, parameters));
        }

        protected T CreateMemberProxy<T>(string subPath)
            where T : class
        {
            return ProxyBuilder.CreateProxy<T>(_executor, _instanceId, _pathPrefix + subPath);
        }

        protected async Task<T> CreateDetachedProxy<T>(string subPath, IDictionary<string, string> query)
            where T : class
        {
            var instanceId = await _executor.Execute(_pathPrefix + subPath, query);
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            return ProxyBuilder.CreateProxy<T>(_executor, instanceId);
        }

        protected IDictionary<string, string> CreateQueryBuilder()
        {
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(_instanceId))
            {
                dict[TinyProtocol.InstanceIdParameterName] = _instanceId;
            }
            return dict;
        }

        protected string RegisterCallbackInstance<T>(T instance)
            where T : class
        {
            return _executor.RegisterCallbackInstance(new SimpleDispatcher<T>(instance));
        }
    }
}
