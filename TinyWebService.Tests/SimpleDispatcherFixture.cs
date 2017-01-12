using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Services;
using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;
using TinyWebService.Service;
using TinyWebService.Tests.Stubs;

namespace TinyWebService.Tests
{
    [TestFixture]
    public class SimpleDispatcherFixture
    {
        private Mock<IEndpoint> _endpoint;

        [SetUp]
        public void Setup()
        {
            _endpoint = new Mock<IEndpoint>();
        }

        [Test]
        public void ShouldDispatch()
        {
            var root = new Root();
            var dispatcher = DispatcherFactory.CreateDispatcher(root, _endpoint.Object, false);

            dispatcher.Execute("BooleanValue/Value", new Dictionary<string, string>()).Result.ShouldBe("False");
            dispatcher.Execute("BooleanValue/UpdateValue", new Dictionary<string, string> { { "value", "True" } });
            root.BooleanValue.Value.ShouldBe(true);

            dispatcher.Execute("IntValue/Value", new Dictionary<string, string>()).Result.ShouldBe("0");
            dispatcher.Execute("IntValue/UpdateValue", new Dictionary<string, string> { { "value", "3" } });
            root.IntValue.Value.ShouldBe(3);

            dispatcher.Execute("StringValue/Value", new Dictionary<string, string>()).Result.ShouldBe(string.Empty);
            dispatcher.Execute("StringValue/UpdateValue", new Dictionary<string, string> { { "value", "3" } });
            root.StringValue.Value.ShouldBe("3");

            dispatcher.Execute("Combine", new Dictionary<string, string> { { "delimiter", "___"} }).Result.ShouldBe("3___3");

            _endpoint.Setup(x => x.RegisterInstance(It.IsAny<object>())).Returns("i1");
            dispatcher.Execute("CreateContainer", new Dictionary<string, string> { { "initialValue", "a" } }).Result.ShouldBe("i1");
        }

        [Test]
        public void ShouldDispatchOnDispatcherThreadForDispatcherObjects()
        {
            using (var dt = new DispatcherThread())
            {
                var root = dt.Invoke(() => new DispatcherRoot());
                var dispatcher = DispatcherFactory.CreateDispatcher(root, _endpoint.Object, false);

                dispatcher.Execute("GetIntValue", new Dictionary<string, string>()).Result.ShouldBe("1");
                dispatcher.Execute("GetIntValueAsync", new Dictionary<string, string>()).Result.ShouldBe("1");

                dispatcher.Execute("Invoke", new Dictionary<string, string>()).Result.ShouldBe("");
                dispatcher.Execute("InvokeAsync", new Dictionary<string, string>()).Result.ShouldBe("");

                _endpoint.Setup(x => x.RegisterInstance(It.IsAny<object>())).Returns("i1");
                dispatcher.Execute("Clone", new Dictionary<string, string>()).Result.ShouldBe("i1");
                dispatcher.Execute("CloneAsync", new Dictionary<string, string>()).Result.ShouldBe("i1");
            }
        }

        [Test]
        public void ShouldThrowOnInvalidPath()
        {
            var root = new Root();
            var dispatcher = DispatcherFactory.CreateDispatcher(root, null, false);

            var task = dispatcher.Execute("IntValue/InvalidPath", new Dictionary<string, string>());
            new Action(() => { task.Wait(); }).ShouldThrow<Exception>();
        }

        [Test]
        public void ShouldFailTaskOnExceptionInDispatchedCode()
        {
            var root = new Root();
            var dispatcher = DispatcherFactory.CreateDispatcher(root, null, false);

            var task = dispatcher.Execute("Throw", new Dictionary<string, string>());
            new Action(() => { task.Wait(); }).ShouldThrow<Exception>();
        }

        [Test]
        public void ShouldDetachInstanceOnCorrepondingCommand()
        {
            var root = new Root();
            var dispatcher = DispatcherFactory.CreateDispatcher(root, _endpoint.Object, false);

            _endpoint.Setup(x => x.RegisterInstance(root)).Returns("instance");
            dispatcher.Execute("~detach", new Dictionary<string, string>()).Result.ShouldBe("instance");
            _endpoint.Verify(x => x.RegisterInstance(root));
        }

        [Test]
        public void ShouldResolveAmbiguityByDiscardingIncompatibleMethods()
        {
            var service = new Mock<IAmbiguousMethodsService>();
            var dispatcher = DispatcherFactory.CreateDispatcher(service.Object, null, false);

            dispatcher.Execute("Execute", new Dictionary<string, string> { { "arg", "value" } });
            service.Verify(x => x.Execute("value"));
        }
    }
}
