using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyWebService.Protocol;
using TinyWebService.Utilities;

namespace TinyWebService.Service
{
    internal sealed class Session : IDisposable
    {
        private static readonly IDictionary<string, string> EmptyDictionary = new Dictionary<string, string>();

        private readonly ConcurrentDictionary<string, RegisteredInstance> _instances = new ConcurrentDictionary<string, RegisteredInstance>();
        private readonly ISimpleDispatcher _rootInstance;
        private readonly ITimer _cleanupTimer;
        private readonly TimeSpan _defaultExpirationTime;

        public Session(ITimer cleanupTimer, TimeSpan defaultExpirationTime, ISimpleDispatcher rootInstance = null)
        {
            _rootInstance = rootInstance;
            _cleanupTimer = cleanupTimer;
            _defaultExpirationTime = defaultExpirationTime;
            _cleanupTimer.Tick += CleanupTimer_Tick;
        }

        public void Dispose()
        {
            _cleanupTimer.Tick -= CleanupTimer_Tick;
            _cleanupTimer.Dispose();
        }

        public string RegisterInstance(ISimpleDispatcher dispatcher)
        {
            var instanceId = Guid.NewGuid().ToString("N");
            _instances[instanceId] = new RegisteredInstance(dispatcher)
            {
                RetentionTime = _defaultExpirationTime,
                ExpirationTime = _cleanupTimer.NextUptime + _defaultExpirationTime
            };
            return instanceId;
        }

        private void CleanupTimer_Tick()
        {
            var toClean = _instances.Where(x => x.Value.ExpirationTime <= _cleanupTimer.Uptime).Select(x => x.Key).ToList();
            foreach (var id in toClean)
            {
                RegisteredInstance instance;
                if (_instances.TryRemove(id, out instance))
                {
                    instance.Dispatcher.Dispose();
                }
            }
        }

        public Task<string> Execute(string absolutePath, string query)
        {
            var index = absolutePath.IndexOf("/", 1);
            var path = absolutePath.Substring(index + 1);

            var parameters = ParseQuery(query);
            string instanceId;
            parameters.TryGetValue(TinyProtocol.InstanceIdParameterName, out instanceId);

            if (path == TinyProtocol.MetadataPath)
            {
                return Tasks.FromResult("<meta/>");
            }

            var dispatcher = _rootInstance;
            if (!string.IsNullOrEmpty(instanceId))
            {
                var instance = _instances[instanceId];
                if (path == TinyProtocol.KeepAlivePath)
                {
                    instance.RetentionTime = TimeSpan.FromDays(100000);
                    instance.ExpirationTime = _cleanupTimer.NextUptime + instance.RetentionTime;
                    return Tasks.FromResult(string.Empty);
                }

                if (path == TinyProtocol.DisposePath)
                {
                    if (_instances.TryRemove(instanceId, out instance))
                    {
                        instance.Dispatcher.Dispose();
                    }
                    return Tasks.FromResult(string.Empty);
                }
                else
                {
                    instance.ExpirationTime = _cleanupTimer.NextUptime + instance.RetentionTime;
                    dispatcher = instance.Dispatcher;
                }
            }
            else if (dispatcher == null)
            {
                throw new InvalidOperationException("Callback operation must specify instanceId");
            }

            return dispatcher.Execute(path, parameters);
        }

        private static IDictionary<string, string> ParseQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return EmptyDictionary;
            }

            return query
                .TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x[1]);
        }

        private sealed class RegisteredInstance
        {
            public RegisteredInstance(ISimpleDispatcher dispatcher)
            {
                Dispatcher = dispatcher;
            }

            public ISimpleDispatcher Dispatcher { get; }
            public TimeSpan RetentionTime { get; set; }
            public TimeSpan ExpirationTime { get; set; }
        }
    }
}