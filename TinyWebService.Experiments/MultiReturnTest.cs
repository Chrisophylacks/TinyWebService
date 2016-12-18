using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyWebService.Experiments
{
    static class MultiReturnTest
    {
        public static void Run()
        {
            var service = new ValueContainer<IEnumerable<IValueContainer<int>>>();
            service.UpdateValue(
                new[]
                {
                    new ValueContainer<int>(),
                    new ValueContainer<int>()
                });

            using (TinyService.Host(service).AtEndpoint("test"))
            {
                var client = TinyClient.Create<IValueContainer<IEnumerable<IValueContainer<int>>>>("test");
                var containers = client.Value.ToList();
                for (int i = 0; i < containers.Count; ++i)
                {
                    containers[i].UpdateValue(i);
                    Console.WriteLine(service.Value.ElementAt(i).Value);
                    Console.WriteLine(containers[i].Value);
                }
            }
        }
    }
}