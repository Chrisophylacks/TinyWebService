using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var par = new Dictionary<string, string> { { "~i", "instance" } };

            _executor.Setup(x => x.Execute("a/b/IntValue/CurrentValue", par)).Returns(Task.FromResult("1"));
            _executor.Setup(x => x.Execute("a/b/StringValue/CurrentValue", par)).Returns(Task.FromResult("a"));

            var proxy = ProxyBuilder.CreateProxy<IDynamicRoot>(_executor.Object, "instance", "a/b");
            var intValue = proxy.IntValue.CurrentValue;
            var stringValue = proxy.StringValue.CurrentValue;

            _executor.Verify(x => x.Execute("a/b/IntValue/CurrentValue", par), Times.Once());
            _executor.Verify(x => x.Execute("a/b/StringValue/CurrentValue", par), Times.Once());

            intValue.ShouldBe(1);
            stringValue.ShouldBe("a");
        }

        [Test]
        public void ShouldSerializeNulls()
        {
            _executor.Setup(x => x.Execute("Clone", new Dictionary<string, string>())).Returns(Task.FromResult(""));

            var proxy = ProxyBuilder.CreateProxy<IRoot>(_executor.Object);
            proxy.Clone().ShouldBe(null);

            _executor.Verify(x => x.Execute("Clone", new Dictionary<string, string>()), Times.Once());
        }

        [Test]
        public void ShouldSerializeStringNulls()
        {
            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", null } };
            _executor.Setup(x => x.Execute("a/StringValue/Value", par)).Returns(Task.FromResult(""));

            var proxy = ProxyBuilder.CreateProxy<IRoot>(_executor.Object, "instance", "a");
            proxy.StringValue.Value.ShouldBe(string.Empty);

            proxy.StringValue.UpdateValue(null);
            _executor.Verify(x => x.Execute("a/StringValue/Value", par), Times.Once());
            _executor.Verify(x => x.Execute("a/StringValue/UpdateValue", par2), Times.Once());
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
            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", serializedValue } };

            _executor.Setup(x => x.Execute("UpdateValue", par2)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value", par)).Returns(Task.FromResult(serializedValue));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<T>>(_executor.Object, "instance");
            proxy.UpdateValue(value);
            proxy.Value.ShouldBe(value);

            _executor.Verify(x => x.Execute("UpdateValue", par2), Times.Once());
            _executor.Verify(x => x.Execute("Value", par), Times.Once());
        }

        [Test]
        public void ShouldSerializeCollection()
        {
            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", "[1,2,3]" } };

            _executor.Setup(x => x.Execute("UpdateValue", par2)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value", par)).Returns(Task.FromResult("[1,2,3]"));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<IEnumerable<int>>>(_executor.Object, "instance");
            proxy.UpdateValue(new[] { 1, 2, 3 });
            proxy.Value.ShouldBe(new[] { 1, 2, 3 });

            _executor.Verify(x => x.Execute("UpdateValue", par2), Times.Once());
            _executor.Verify(x => x.Execute("Value", par), Times.Once());
        }

        [Test]
        public void ShouldSerializeArrayCollection()
        {
            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", "[1,2,3]" } };

            _executor.Setup(x => x.Execute("UpdateValue", par2)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value", par)).Returns(Task.FromResult("[1,2,3]"));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<string[]>>(_executor.Object, "instance");
            proxy.UpdateValue(new[] { "1", "2", "3" });
            proxy.Value.ShouldBe(new[] { "1", "2", "3" });

            _executor.Verify(x => x.Execute("UpdateValue", par2), Times.Once());
            _executor.Verify(x => x.Execute("Value", par), Times.Once());
        }

        [Test]
        public void ShouldSerializeCustomTypes()
        {
            TinyClient.RegisterCustomSerializer(x => Convert.ToInt64(x.TotalMilliseconds).ToString(), x => TimeSpan.FromMilliseconds(long.Parse(x)));

            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", "300000" } };

            _executor.Setup(x => x.Execute("UpdateValue", par2)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value", par)).Returns(Task.FromResult("300000"));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<TimeSpan>>(_executor.Object, "instance");
            proxy.UpdateValue(TimeSpan.FromMinutes(5));
            proxy.Value.ShouldBe(TimeSpan.FromMinutes(5));

            _executor.Verify(x => x.Execute("UpdateValue", par2), Times.Once());
            _executor.Verify(x => x.Execute("Value", par), Times.Once());
        }

        [Test]
        public void ShouldSerializeEnums()
        {
            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", "InvariantCulture" } };

            _executor.Setup(x => x.Execute("UpdateValue", par2)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value", par)).Returns(Task.FromResult("InvariantCulture"));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<StringComparison>>(_executor.Object, "instance");
            proxy.UpdateValue(StringComparison.InvariantCulture);
            proxy.Value.ShouldBe(StringComparison.InvariantCulture);

            _executor.Verify(x => x.Execute("UpdateValue", par2), Times.Once());
            _executor.Verify(x => x.Execute("Value", par), Times.Once());
        }

        [Test]
        public void ShouldSerializeDataObjects()
        {
            const string serializedString = @"{""Key"":""a"",""Nested"":{""Key"":""b"",""Nested"":null}}";
            var par = new Dictionary<string, string> { { "~i", "instance" } };
            var par2 = new Dictionary<string, string> { { "~i", "instance" }, { "value", serializedString } };

            _executor.Setup(x => x.Execute("UpdateValue", par2)).Returns(Task.FromResult(string.Empty));
            _executor.Setup(x => x.Execute("Value", par)).Returns(Task.FromResult(serializedString));

            var proxy = ProxyBuilder.CreateProxy<IValueContainer<DataObject>>(_executor.Object, "instance");
            proxy.UpdateValue(new DataObject
            {
                Key= "a",
                Nested = new DataObject { Key = "b" }
            });

            var value = proxy.Value;
            value.Key.ShouldBe("a");
            value.Nested.Key.ShouldBe("b");
            value.Nested.Nested.ShouldBe(null);

            _executor.Verify(x => x.Execute("UpdateValue", par2), Times.Once());
            _executor.Verify(x => x.Execute("Value", par), Times.Once());
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
