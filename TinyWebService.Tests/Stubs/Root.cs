namespace TinyWebService.Tests.Stubs
{
    public class Root : IRoot
    {
        public Root()
        {
            IntValue = new ValueContainer<int>(0);
            StringValue = new ValueContainer<string>(string.Empty);
        }

        public IValueContainer<int> IntValue { get; private set; }

        public IValueContainer<string> StringValue { get; private set; }

        public string Combine(string delimiter)
        {
            return IntValue.Value + delimiter + StringValue.Value;
        }

        public IValueContainer<string> CreateContainer(string initialValue)
        {
            return new ValueContainer<string>(initialValue);
        }
    }
}