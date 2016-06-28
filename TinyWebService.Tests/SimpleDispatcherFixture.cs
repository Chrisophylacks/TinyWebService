using System;
using System.Collections.Generic;
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
            var dispatcher = new SimpleDispatcher<IRoot>(root);

            dispatcher.Execute("BooleanValue/Value", new Dictionary<string, string>()).ShouldBe("False");
            dispatcher.Execute("BooleanValue/UpdateValue", new Dictionary<string, string> { { "value", "True" } });
            root.BooleanValue.Value.ShouldBe(true);

            dispatcher.Execute("IntValue/Value", new Dictionary<string, string>()).ShouldBe("0");
            dispatcher.Execute("IntValue/UpdateValue", new Dictionary<string, string> { { "value", "3" } });
            root.IntValue.Value.ShouldBe(3);

            dispatcher.Execute("StringValue/Value", new Dictionary<string, string>()).ShouldBe(string.Empty);
            dispatcher.Execute("StringValue/UpdateValue", new Dictionary<string, string> { { "value", "3" } });
            root.StringValue.Value.ShouldBe("3");

            dispatcher.Execute("Combine", new Dictionary<string, string> { { "delimiter", "___"} }).ShouldBe("3___3");
            var container = dispatcher.Execute("CreateContainer", new Dictionary<string, string> { { "initialValue", "a" } });
            container.ShouldBeAssignableTo<ISimpleDispatcher>();

            var containerDispatcher = (ISimpleDispatcher) container;
            containerDispatcher.Execute("Value", new Dictionary<string, string>()).ShouldBe("a");
        }

        [Test]
        public void ShouldThrowOnInvalidPath()
        {
            var root = new Root();
            var dispatcher = new SimpleDispatcher<IRoot>(root);

            new Action(() => { dispatcher.Execute("IntValue/InvalidPath", new Dictionary<string, string>()); }).ShouldThrow<InvalidOperationException>();
        }
    }
}
