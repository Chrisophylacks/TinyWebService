using System;

namespace TinyWebService.Tests.Stubs
{
    public sealed class DynamicValue<T>
    {
        private readonly Func<T> _getValue;

        public DynamicValue(Func<T> getValue)
        {
            _getValue = getValue;
        }

        public T CurrentValue => _getValue();
    }
}