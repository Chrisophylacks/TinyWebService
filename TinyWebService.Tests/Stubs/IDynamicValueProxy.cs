namespace TinyWebService.Tests.Stubs
{
    public interface IDynamicValueProxy<T>
    {
        T CurrentValue { get; }
    }
}