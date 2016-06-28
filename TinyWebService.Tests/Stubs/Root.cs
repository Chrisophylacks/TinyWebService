using System;

namespace TinyWebService.Tests.Stubs
{
    public class Root : IRoot
    {
        public Root()
        {
            IntValue = new ValueContainer<int>(0);
            BooleanValue = new ValueContainer<bool>(false);
            StringValue = new ValueContainer<string>(string.Empty);
            DoubleContainer = new ValueContainer<double>(0.0);
            EnumValue = new ValueContainer<TypeCode>(TypeCode.Boolean);
        }

        public IValueContainer<bool> BooleanValue { get; }

        public IValueContainer<int> IntValue { get; }

        public IValueContainer<string> StringValue { get; }

        public IValueContainer<double> DoubleContainer { get; }

        public IValueContainer<TypeCode> EnumValue { get; }

        public string Combine(string delimiter)
        {
            return IntValue.Value + delimiter + StringValue.Value;
        }

        public IValueContainer<string> CreateContainer(string initialValue)
        {
            return new ValueContainer<string>(initialValue);
        }

        public IRoot Clone()
        {
            return new Root();
        }
    }
}