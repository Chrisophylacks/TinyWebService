namespace TinyWebService.Tests.Stubs
{
    public interface IValueContainer<T>
    {
        T Value { get; }
        void UpdateValue(T value);
    }
}