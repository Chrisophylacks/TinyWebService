using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Client;
using TinyWebService.Tests.Stubs;

namespace TinyWebService.Tests
{
    [TestFixture]
    public sealed class ProxiesFixture
    {
        private Mock<IExecutor> _executor;

        [SetUp]
        public void SetUp()
        {
            ProxyBuilder.RegisterCustomProxyFactory<CustomProxyFactory>();
            _executor = new Mock<IExecutor>();
        }

        [Test]
        public void ShoudSupplyGenericProxy()
        {
            _executor.Setup(x => x.Execute("a/b/IntValue/CurrentValue?instanceId=instance")).Returns("1");
            _executor.Setup(x => x.Execute("a/b/StringValue/CurrentValue?instanceId=instance")).Returns("a");

            var proxy = ProxyBuilder.CreateProxy<IDynamicRoot>(_executor.Object, "instance", "a/b");
            var intValue = proxy.IntValue.CurrentValue;
            var stringValue = proxy.StringValue.CurrentValue;

            _executor.Verify(x => x.Execute("a/b/IntValue/CurrentValue?instanceId=instance"), Times.Once());
            _executor.Verify(x => x.Execute("a/b/StringValue/CurrentValue?instanceId=instance"), Times.Once());

            intValue.ShouldBe(1);
            stringValue.ShouldBe("a");
        }

        [TestCase(1, "1")]
        [TestCase(1.0, "1")]
        [TestCase(1.23, "1.23")]
        [TestCase(1.23f, "1.23")]
        [TestCase("123", "123")]
        [TestCase(1L, "1")]
        [TestCase(12345678901234L, "12345678901234")]
        public void ShouldSerializeSimpleTypes<T>(T value, string serializedValue)
        {
            _executor.Setup(x => x.Execute("UpdateValue?instanceId=instance&value=" + serializedValue)).Returns(string.Empty);
            _executor.Setup(x => x.Execute("Value?instanceId=instance")).Returns(serializedValue);

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<T>>(_executor.Object, "instance");
            proxy.UpdateValue(value);
            proxy.Value.ShouldBe(value);

            _executor.Verify(x => x.Execute("UpdateValue?instanceId=instance&value=" + serializedValue), Times.Once());
            _executor.Verify(x => x.Execute("Value?instanceId=instance"), Times.Once());
        }

        public class CustomProxyFactory
        {
            public static DynamicValue<T> CreateProxy<T>(IDynamicValueProxy<T> proxy)
            {
                return new DynamicValue<T>(() => proxy.CurrentValue);
            }
        }

        public interface IDynamicValueProxy<T>
        {
            T CurrentValue { get; }
        }
    }
}
