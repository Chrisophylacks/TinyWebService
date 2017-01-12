using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Client;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;
using TinyWebService.Tests.Stubs;

namespace TinyWebService.Tests
{
    [TestFixture]
    public sealed class SerializerFixture
    {
        private Mock<IEndpoint> _endpoint;

        [SetUp]
        public void Setup()
        {
            _endpoint = new Mock<IEndpoint>();
            TinyProtocol.RegisterCustomProxyFactory<CustomProxyFactory>();
        }

        [Test]
        public void ShouldSerializePrimitives()
        {
            Serialize(123).ShouldBe("123");
            Serialize(123.4).ShouldBe("123.4");
            Serialize(123.4f).ShouldBe("123.4");
            Serialize(123L).ShouldBe("123");
            Serialize("123").ShouldBe("123");
            Serialize(String.Empty).ShouldBe(String.Empty);

            Deserialize<int>("123").ShouldBe(123);
            Deserialize<double>("123.4").ShouldBe(123.4);
            Deserialize<float>("123.4").ShouldBe(123.4f);
            Deserialize<long>("123").ShouldBe(123L);
            Deserialize<string>("123").ShouldBe("123");
            Deserialize<string>(String.Empty).ShouldBe(String.Empty);
        }

        [Test]
        public void ShouldSerializeCustomProxies()
        {
            var instance = new DynamicValue<int>(null, () => 0);
            _endpoint.Setup(x => x.RegisterInstance(instance)).Returns("i1");
            Serialize(instance).ShouldBe("i1");
            Serialize<DynamicValue<int>>(null).ShouldBe(String.Empty);

            // transparent serialization scenario
            var instance2 = new DynamicValue<int>(TinyProtocol.Serializer<IDynamicValueProxy<int>>.Deserialize(_endpoint.Object, "otherProxy"), () => 0);
            Serialize(instance2).ShouldBe("otherProxy");

            Deserialize<DynamicValue<int>>("i1").ShouldNotBeNull();
            Deserialize<DynamicValue<int>>(String.Empty).ShouldBe(null);
        }

        [Test]
        public void ShouldSerializeStandardProxies()
        {
            var instance = new ValueContainer<int>(0);
            _endpoint.Setup(x => x.RegisterInstance(instance)).Returns("i1");
            Serialize<IValueContainer<int>>(instance).ShouldBe("i1");
            Serialize<IValueContainer<int>>(null).ShouldBe(String.Empty);

            Deserialize<DynamicValue<int>>("i1").ShouldNotBeNull();
            Deserialize<IValueContainer<int>>(String.Empty).ShouldBe(null);
        }

        [Test]
        public void ShouldSerializeCollection()
        {
            Serialize<IEnumerable<string>>(new[] { "1", "2", "3"}).ShouldBe("[1,2,3]");
            Serialize<IList<string>>(new[] { "1", "2", "3" }).ShouldBe("[1,2,3]");
            Serialize<string[]>(new[] { "1", "2", "3" }).ShouldBe("[1,2,3]");
            Serialize<string[]>(new string[0]).ShouldBe("[]");
            Serialize<string[]>(null).ShouldBe("");

            Deserialize<IEnumerable<string>>("[1,2,3]").ShouldBe(new[] { "1", "2", "3" });
            Deserialize<IList<string>>("[1,2,3]").ShouldBe(new[] { "1", "2", "3" });
            Deserialize<string[]>("[1,2,3]").ShouldBe(new[] { "1", "2", "3" });
            Deserialize<string[]>("[]").ShouldBe(new string[0]);
            Deserialize<string[]>("").ShouldBeNull();
        }

        [Test]
        public void ShouldSerializeCustomTypes()
        {
            TinyClient.RegisterCustomSerializer(x => Convert.ToInt64(x.TotalMilliseconds).ToString(), x => TimeSpan.FromMilliseconds(long.Parse(x)));
            Serialize(TimeSpan.FromSeconds(1.234)).ShouldBe("1234");
            Deserialize<TimeSpan>("1234").TotalMilliseconds.ShouldBe(1234);
        }

        [Test]
        public void ShouldSerializeEnums()
        {
            Deserialize<StringComparison>("InvariantCulture").ShouldBe(StringComparison.InvariantCulture);
            Serialize<StringComparison>(StringComparison.InvariantCulture).ShouldBe("InvariantCulture");
        }

        [Test]
        public void ShouldSerializeDataObjects()
        {
            const string serializedString = @"{""Key"":""a"",""Nested"":{""Key"":""b"",""Nested"":null}}";

            var value = Deserialize<DataObject>(serializedString);
            value.Key.ShouldBe("a");
            value.Nested.Key.ShouldBe("b");
            value.Nested.Nested.ShouldBe(null);

            Serialize<DataObject>(new DataObject
            {
                Key = "a",
                Nested = new DataObject { Key = "b" }
            }).ShouldBe(serializedString);
        }

        private string Serialize<T>(T value)
        {
            return TinyProtocol.Serializer<T>.Serialize(_endpoint.Object, value);
        }

        private T Deserialize<T>(string value)
        {
            return TinyProtocol.Serializer<T>.Deserialize(_endpoint.Object, value);
        }
    }
}
