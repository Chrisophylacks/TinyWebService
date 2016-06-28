using System;

namespace TinyWebService.Tests.Stubs
{
    public interface IRoot
    {
        IValueContainer<bool> BooleanValue { get; }

        IValueContainer<int> IntValue { get; }

        IValueContainer<string> StringValue { get; }

        IValueContainer<TypeCode> EnumValue { get; }

        string Combine(string delimiter);

        IValueContainer<string> CreateContainer(string initialValue);

        IValueContainer<double> DoubleContainer { get; }

        IRoot Clone();
    }
}