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

        public IValueContainer<bool> BooleanValue { get; private set; }

        public IValueContainer<int> IntValue { get; private set; }

        public IValueContainer<string> StringValue { get; private set; }

        public IValueContainer<double> DoubleContainer { get; private set; }

        public IValueContainer<TypeCode> EnumValue { get; private set; }

        public string Combine(string delimiter)
        {
            return IntValue.Value + delimiter + StringValue.Value;
        }

        public IValueContainer<string> CreateContainer(string initialValue)
        {
            return new ValueContainer<string>(initialValue);
        }

        public void Throw()
        {
            throw new Exception();
        }

        public IRoot Clone()
        {
            return new Root();
        }
    }
}