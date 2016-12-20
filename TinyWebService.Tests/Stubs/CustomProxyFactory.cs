namespace TinyWebService.Tests.Stubs
{
    public class CustomProxyFactory
    {
        public static DynamicValue<T> CreateProxy<T>(IDynamicValueProxy<T> proxy)
        {
            return new DynamicValue<T>(proxy, () => proxy.CurrentValue);
        }
    }
}