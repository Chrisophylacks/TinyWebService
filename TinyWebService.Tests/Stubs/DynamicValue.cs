using System;

namespace TinyWebService.Tests.Stubs
{
    public sealed class DynamicValue<T> : IRemotableInstance
    {
        private readonly Func<T> _getValue;
        private readonly object _realProxy;

        public DynamicValue(object realProxy, Func<T> getValue)
        {
            _getValue = getValue;
            _realProxy = realProxy;
        }

        public T CurrentValue => _getValue();

        object IRemotableInstance.RealProxy => _realProxy;
    }
}