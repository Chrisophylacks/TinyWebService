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

            public TimeSpan Timeout
            {
                get { return _innerExecutor.Timeout; }
                set { _innerExecutor.Timeout = value; }
            }
        }
    }
}
