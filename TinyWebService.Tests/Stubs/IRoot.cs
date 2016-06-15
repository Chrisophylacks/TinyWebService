namespace TinyWebService.Tests.Stubs
{
    public interface IRoot
    {
        IValueContainer<int> IntValue { get; }

        IValueContainer<string> StringValue { get; }

        string Combine(string delimiter);

        IValueContainer<string> CreateContainer(string initialValue);
    }
}