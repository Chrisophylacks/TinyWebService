using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyWebService.Experiments
{
    public interface IValueContainer<T>
    {
        T Value { get; }
        void UpdateValue(T value);
    }

    class ValueContainer<T> : IValueContainer<T>
    {
        public T Value { get; set; }

        public void UpdateValue(T value)
        {
            Value = value;
        }
    }

    static class NestedTest
    {
        public static void Run()
        {
            var service = new ValueContainer<ValueContainer<string>>();
            service.UpdateValue(new ValueContainer<string>());

            using (TinyService.Host(service).AtEndpoint("test"))
            {
                var client = TinyClient.Create<IValueContainer<IValueContainer<string>>>("test");
                client.Value.UpdateValue("abc");
                client.Value.UpdateValue("cde");
                Console.WriteLine(client.Value.Value);
            }
        }
    }
}
