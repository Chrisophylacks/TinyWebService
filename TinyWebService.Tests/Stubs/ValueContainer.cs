namespace TinyWebService.Tests.Stubs
{
    public class ValueContainer<T> : IValueContainer<T>
    {
        public ValueContainer(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public void UpdateValue(T value)
        {
            Value = value;
        }
    }
}