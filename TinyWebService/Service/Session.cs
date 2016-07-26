using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TinyWebService.Protocol;
using TinyWebService.Utilities;

namespace TinyWebService.Service
{
    internal sealed class Session : IDisposable
    {
        private readonly ConcurrentDictionary<string, RegisteredInstance> _instances = new ConcurrentDictionary<string, RegisteredInstance>();
        private readonly ISimpleDispatcher _rootInstance;
        private readonly ITimer _cleanupTimer;
        private long _currentOperationId;
        private long _cleanupOperationId;

        public Session(ITimer cleanupTimer, ISimpleDispatcher rootInstance = null)
        {
            _rootInstance = rootInstance;
            _cleanupTimer = cleanupTimer;
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
            _instances[instanceId] = new RegisteredInstance(dispatcher, Interlocked.Increment(ref _currentOperationId));
            return instanceId;
        }

        private void CleanupTimer_Tick()
        {
            var toClean = _instances.Where(x => x.Value.LastOperationId <= _cleanupOperationId).Select(x => x.Key).ToList();
            foreach (var id in toClean)
            {
                RegisteredInstance instance;
                if (_instances.TryRemove(id, out instance))
                {
                    instance.Dispatcher.Dispose();
                }
            }

            _cleanupOperationId = Interlocked.Read(ref _currentOperationId);
        }

        public async Task<string> Execute(string absolutePath, string query)
        {
            var index = absolutePath.IndexOf("/", 1);
            var path = absolutePath.Substring(index + 1);

            if (path == TinyProtocol.MetadataPath)
            {
                return "<meta/>";
            }

            var parameters = ParseQuery(query);
            string instanceId;
            parameters.TryGetValue(TinyProtocol.InstanceIdParameterName, out instanceId);
            var operationId = Interlocked.Increment(ref _currentOperationId);

            var dispatcher = _rootInstance;
            if (!string.IsNullOrEmpty(instanceId))
            {
                var instance = _instances[instanceId];
                instance.LastOperationId = operationId;
                dispatcher = instance.Dispatcher;
            }
            else if (dispatcher == null)
            {
                throw new InvalidOperationException("Callback operation must specify instanceId");
            }

            var result = await dispatcher.Execute(path, parameters);

            var newInstance = result as ISimpleDispatcher;
            if (newInstance != null)
            {
                return RegisterInstance(newInstance);
            }

            return (string)result;
        }

        private static IDictionary<string, string> ParseQuery(string query)
        {
            return query
                .TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x[1]);
        }

        private sealed class RegisteredInstance
        {
            public RegisteredInstance(ISimpleDispatcher dispatcher, long operationId)
            {
                Dispatcher = dispatcher;
                LastOperationId = operationId;
            }

            public ISimpleDispatcher Dispatcher { get; }
            public long LastOperationId { get; set; }
        }
    }
}