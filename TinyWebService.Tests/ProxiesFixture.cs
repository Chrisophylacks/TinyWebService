using System;
using System.Collections.Generic;
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
    public sealed class ProxiesFixture
    {
        private const string Prefix = "test://endpoint";

        private Mock<IExecutor> _executor;
        private Mock<IEndpoint> _endpoint;

        [SetUp]
        public void SetUp()
        {
            TinyProtocol.RegisterCustomProxyFactory<CustomProxyFactory>();
            _executor = new Mock<IExecutor>();
            _endpoint = new Mock<IEndpoint>();
            _endpoint.Setup(x => x.GetExecutor(It.IsAny<string>(), It.IsAny<TimeSpan?>())).Returns<string, TimeSpan?>((x, t) => new PrefixedExecutor(x, _executor.Object));
        }

        [Test]
        public void ShouldThrowOnGenericMethods()
        {
            var proxy = CreateProxy<IGenericFinder>();
            new Action(() => { proxy.Find<string>("key"); }).ShouldThrow<InvalidOperationException>();
            new Action(() => { proxy.Find<int>("key"); }).ShouldThrow<InvalidOperationException>();
            new Action(() => { proxy.Test<string>(); }).ShouldThrow<InvalidOperationException>();
            new Action(() => { proxy.Test<int>(); }).ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ShouldThrowOnMethodWithNonSerializableParameters()
        {
            var proxy = CreateProxy<IGenericFinder>();
            new Action(() => { proxy.Find(1m); }).ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ShouldCreateCustomProxy()
        {
            var proxy = CreateProxy<DynamicValue<int>>();
            proxy.CurrentValue.ShouldBe(0);
            _executor.Verify(x => x.Execute(Prefix + "/CurrentValue", new Dictionary<string, string>()));
        }

        [Test]
        public void ShouldCreateCustomMemberProxy()
        {
            var proxy = CreateProxy<IDynamicRoot>();
            proxy.IntValue.CurrentValue.ShouldBe(0);
            _executor.Verify(x => x.Execute(Prefix + "/IntValue/CurrentValue", new Dictionary<string, string>()));
        }

        private T CreateProxy<T>(string instanceId = null, string path = null)
        {
            return TinyProtocol.Serializer<T>.Deserialize(_endpoint.Object, new ObjectAddress(string.IsNullOrEmpty(path) ? Prefix : Prefix + "/" + path, instanceId).Encode());
        }

        private sealed class PrefixedExecutor : IExecutor
        {
            private readonly string _prefix;
            private readonly IExecutor _innerExecutor;

            public PrefixedExecutor(string prefix, IExecutor innerExecutor)
            {
                _prefix = prefix;
                _innerExecutor = innerExecutor;
            }

            public Task<string> Execute(string pathAndQuery, IDictionary<string, string> parameters = null)
            {
                return _innerExecutor.Execute(_prefix + pathAndQuery, parameters);
            }
        }
    }
}
