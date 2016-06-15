using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TinyWebService.Service
{
    internal sealed class Session
    {
        private readonly IDictionary<string, ISimpleDispatcher> _instances = new ConcurrentDictionary<string, ISimpleDispatcher>();
        private readonly ISimpleDispatcher _rootInstance;

        private long _nextInstanceId;

        public Session(ISimpleDispatcher rootInstance)
        {
            _rootInstance = rootInstance;
        }

        public string Execute(string instanceId, string path, IDictionary<string, string> parameters)
        {
            var instance = string.IsNullOrEmpty(instanceId) ? _rootInstance : _instances[instanceId];
            var result = instance.Execute(path, parameters);

            var newInstance = result as ISimpleDispatcher;
            if (newInstance != null)
            {
                var newInstanceId = Interlocked.Increment(ref _nextInstanceId).ToString();
                _instances[newInstanceId] = newInstance;
                return newInstanceId;
            }

            return (string)result;
        }
    }
}