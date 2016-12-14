using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Shouldly;
using TinyWebService.Service;
using TinyWebService.Tests.Stubs;

namespace TinyWebService.Tests
{
    [TestFixture]
    public class SimpleDispatcherFixture
    {
        [Test]
        public void ShouldDispatch()
        {
            var root = new Root();
            var dispatcher = new SimpleDispatcher<IRoot>(root, false);

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
            var container = dispatcher.Execute("CreateContainer", new Dictionary<string, string> { { "initialValue", "a" } }).Result;
            container.ShouldBeAssignableTo<ISimpleDispatcher>();

            var containerDispatcher = (ISimpleDispatcher) container;
            containerDispatcher.Execute("Value", new Dictionary<string, string>()).Result.ShouldBe("a");
        }

        [Test]
        public void ShouldDispatchOnDispatcherThreadForDispatcherObjects()
        {
            using (var dt = new DispatcherThread())
            {
                var root = dt.Invoke(() => new DispatcherRoot());
                var dispatcher = new SimpleDispatcher<IMethodRoot>(root, false);

                dispatcher.Execute("GetIntValue", new Dictionary<string, string>()).Result.ShouldBe("1");
                dispatcher.Execute("GetIntValueAsync", new Dictionary<string, string>()).Result.ShouldBe("1");

                dispatcher.Execute("Invoke", new Dictionary<string, string>()).Result.ShouldBe("");
                dispatcher.Execute("InvokeAsync", new Dictionary<string, string>()).Result.ShouldBe("");

                dispatcher.Execute("Clone", new Dictionary<string, string>()).Result.ShouldBeOfType<SimpleDispatcher<IMethodRoot>>();
                dispatcher.Execute("CloneAsync", new Dictionary<string, string>()).Result.ShouldBeOfType<SimpleDispatcher<IMethodRoot>>();
            }
        }

        [Test]
        public void ShouldThrowOnInvalidPath()
        {
            var root = new Root();
            var dispatcher = new SimpleDispatcher<IRoot>(root, false);

            new Action(() => { dispatcher.Execute("IntValue/InvalidPath", new Dictionary<string, string>()); }).ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ShouldResolveAmbiguityByDiscardingIncompatibleMethods()
        {
            var service = new Mock<IAmbiguousMethodsService>();
            var dispatcher = new SimpleDispatcher<IAmbiguousMethodsService>(service.Object, false);

            dispatcher.Execute("Execute", new Dictionary<string, string> { { "arg", "value" } });
            service.Verify(x => x.Execute("value"));
        }
    }
}
