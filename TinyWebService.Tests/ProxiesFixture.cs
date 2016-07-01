using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
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
        public void ShouldSupplyGenericProxy()
        {
            _executor.Setup(x => x.Execute("a/b/IntValue/CurrentValue?instanceId=instance")).Returns(Task.FromResult("1"));
            _executor.Setup(x => x.Execute("a/b/StringValue/CurrentValue?instanceId=instance")).Returns(Task.FromResult("a"));

            var proxy = ProxyBuilder.CreateProxy<IDynamicRoot>(_executor.Object, "instance", "a/b");
            var intValue = proxy.IntValue.CurrentValue;
            var stringValue = proxy.StringValue.CurrentValue;

            _executor.Verify(x => x.Execute("a/b/IntValue/CurrentValue?instanceId=instance"), Times.Once());
            _executor.Verify(x => x.Execute("a/b/StringValue/CurrentValue?instanceId=instance"), Times.Once());

            intValue.ShouldBe(1);
            stringValue.ShouldBe("a");
        }

        [Test]
        public void ShouldSerializeNulls()
        {
            _executor.Setup(x => x.Execute("Clone")).Returns(Task.FromResult(""));

            var proxy = ProxyBuilder.CreateProxy<IRoot>(_executor.Object);
            proxy.Clone().ShouldBe(null);

            _executor.Verify(x => x.Execute("Clone"), Times.Once());
        }

        [Test]
        public void ShouldSerializeStringNulls()
        {
            _executor.Setup(x => x.Execute("a/StringValue/Value?instanceId=instance")).Returns(Task.FromResult(""));

            var proxy = ProxyBuilder.CreateProxy<IRoot>(_executor.Object, "instance", "a");
            proxy.StringValue.Value.ShouldBe(string.Empty);

            proxy.StringValue.UpdateValue(null);
            _executor.Verify(x => x.Execute("a/StringValue/Value?instanceId=instance"), Times.Once());
            _executor.Verify(x => x.Execute("a/StringValue/UpdateValue?instanceId=instance&value="), Times.Once());
        }

        [Test]
        public void ShouldThrowOnGenericMethods()
        {
            var proxy = ProxyBuilder.CreateProxy<IGenericFinder>(_executor.Object);
            new Action(() => { proxy.Find<string>("key"); }).ShouldThrow<InvalidOperationException>();
            new Action(() => { proxy.Find<int>("key"); }).ShouldThrow<InvalidOperationException>();
            new Action(() => { proxy.Test<string>(); }).ShouldThrow<InvalidOperationException>();
            new Action(() => { proxy.Test<int>(); }).ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ShouldThrowOnMethodWithNonSerializableParameters()
        {
            var proxy = ProxyBuilder.CreateProxy<IGenericFinder>(_executor.Object);
            new Action(() => { proxy.Find(1m); }).ShouldThrow<InvalidOperationException>();
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
            _executor.Setup(x => x.Execute("UpdateValue?instanceId=instance&value=" + serializedValue)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value?instanceId=instance")).Returns(Task.FromResult(serializedValue));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<T>>(_executor.Object, "instance");
            proxy.UpdateValue(value);
            proxy.Value.ShouldBe(value);

            _executor.Verify(x => x.Execute("UpdateValue?instanceId=instance&value=" + serializedValue), Times.Once());
            _executor.Verify(x => x.Execute("Value?instanceId=instance"), Times.Once());
        }

        [Test]
        public void ShouldSerializeCollection()
        {
            _executor.Setup(x => x.Execute("UpdateValue?instanceId=instance&value=[1,2,3]")).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value?instanceId=instance")).Returns(Task.FromResult("[1,2,3]"));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<IEnumerable<int>>>(_executor.Object, "instance");
            proxy.UpdateValue(new[] { 1, 2, 3 });
            proxy.Value.ShouldBe(new[] { 1, 2, 3 });

            _executor.Verify(x => x.Execute("UpdateValue?instanceId=instance&value=[1,2,3]"), Times.Once());
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
