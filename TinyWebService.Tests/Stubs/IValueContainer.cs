namespace TinyWebService.Tests.Stubs
{
    public interface IUpdatable<T>
    {
        void UpdateValue(T value);
    }

    public interface IValueContainer<T> : IUpdatable<T>
    {
        T Value { get; }
    }
}