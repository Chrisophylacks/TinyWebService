using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Client;
using TinyWebService.Tests.Stubs;

namespace TinyWebService.Tests
{
    [TestFixture]
    public sealed class CustomProxiesFixture
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
