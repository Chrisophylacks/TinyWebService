using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Tests.Services;

namespace TinyWebService.Tests.Integration
{
    [TestFixture]
    class IntegrationFixture
    {
        [Test]
        public void NullablesTest()
        {
            var service = new Mock<INullableTestService>();
            service.Setup(x => x.Test(null)).Returns<double?>(null);
            service.Setup(x => x.Test(1.23)).Returns(1.23);

            using (TinyService.Host(service.Object).AtEndpoint("test"))
            {
                var client = TinyClient.Create<INullableTestService>("test");
                client.Test(null).ShouldBe(null);
                client.Test(1.23).ShouldBe(1.23);
            }
        }

        [Test]
        public void NoServiceTest()
        {
            Should.Throw<TinyWebServiceException>(() => TinyClient.Create<INullableTestService>("test"));
        }
    }
}
