namespace TinyWebService.Tests.Stubs
{
    public interface IDynamicRoot
    {
        DynamicValue<int> IntValue { get; }

        DynamicValue<string> StringValue { get; }
    }
}