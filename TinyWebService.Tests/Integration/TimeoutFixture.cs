using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Tests.Services;

namespace TinyWebService.Tests.Integration
{
    [TestFixture]
    class TimeoutFixture
    {
        [Test]
        public void ShouldNotThrowIfNotTimedOut()
        {
            var service = new Mock<INullableTestService>();
            service.Setup(x => x.Test(null)).Returns<double?>(x =>
            {
                Thread.Sleep(2000);
                return null;
            });

            using (TinyService.Host(service.Object).AtEndpoint("test"))
            {
                TinyClient.InitDefaultEndpoint(executionTimeout: TimeSpan.FromSeconds(3));
                var client = TinyClient.Create<INullableTestService>("test");
                client.Test(null).ShouldBe(null);
            }
        }

        [Test]
        public void ShouldThrowIfTimedOut()
        {
            var service = new Mock<INullableTestService>();
            service.Setup(x => x.Test(null)).Returns<double?>(x =>
            {
                Thread.Sleep(2000);
                return null;
            });

            using (TinyService.Host(service.Object).AtEndpoint("test"))
            {
                TinyClient.InitDefaultEndpoint(executionTimeout: TimeSpan.FromSeconds(1));
                var client = TinyClient.Create<INullableTestService>("test");
                Should.Throw<TimeoutException>(() => client.Test(null));
            }
        }
    }
}