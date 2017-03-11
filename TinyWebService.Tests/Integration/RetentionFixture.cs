using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Tests.Services;

namespace TinyWebService.Tests.Integration
{
    [TestFixture]
    class RetentionFixture
    {
        private IDisposable endpoint;
        private IRetentionService client;

        [SetUp]
        public void SetUp()
        {
            var service = new Mock<IRetentionService>();
            service.Setup(x => x.GetInstance(It.IsAny<string>())).Returns<string>(text =>
            {
                var instance = new Mock<IRetentionInstance>();
                instance.Setup(x => x.Call()).Returns(text);
                return instance.Object;
            });

            endpoint = TinyService.Host(service.Object).WithCleanupInterval(TimeSpan.FromSeconds(2)).AtEndpoint("test");
            client = TinyClient.Create<IRetentionService>("test");
        }

        [TearDown]
        public void TearDown()
        {
            endpoint.Dispose();
        }

        [Test]
        public void ShouldCollectUnusedInstances()
        {
            var instance = client.GetInstance("1");
            instance.Call().ShouldBe("1");
            Thread.Sleep(5000);
            Should.Throw<Exception>(() => instance.Call());
        }

        [Test]
        public void ShouldKeepUsedInstances()
        {
            var instance = client.GetInstance("1");
            for (int i = 0; i < 8; ++i)
            {
                Thread.Sleep(1000);
                instance.Call().ShouldBe("1");
            }
        }

        [Test]
        public void ShouldKeepDesignatedInstances()
        {
            var instance = client.GetInstance("1");
            TinyClient.KeepAlive(instance);
            Thread.Sleep(5000);
            instance.Call().ShouldBe("1");
        }

        [Test]
        public void ShouldCollectDisposedInstances()
        {
            var instance = client.GetInstance("1");
            instance.Call().ShouldBe("1");
            TinyClient.Dispose(instance);
            Should.Throw<Exception>(() => instance.Call());
        }
    }
}
