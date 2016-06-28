using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TinyWebService.Experiments
{
    public interface IValueHolder<T>
    {
        T Value { get; }
        void Update(T value);
    }

    public interface IRoot
    {
        IValueHolder<int> First { get; }
        IValueHolder<int> Second { get; }

        string Combine(string separator);

        IRoot Clone();

        string Environment { get; }
    }

    public sealed class ValueHolder<T> : IValueHolder<T>
    {
        public ValueHolder(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public void Update(T value)
        {
            Value = value;
        }
    }

    public sealed class Root : IRoot
    {
        public Root(string environment)
        {
            Environment = environment;
            First = new ValueHolder<int>(3);
            Second = new ValueHolder<int>(7);
        }

        public IValueHolder<int> First { get; private set; }

        public IValueHolder<int> Second { get; private set; }

        public string Combine(string separator)
        {
            throw new Exception("aaa");
            return First.Value + separator + Second.Value;
        }

        public IRoot Clone()
        {
            return new Root(Environment + "_clone");
        }

        public string Environment { get; private set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //using (TinyService.Run(new Root("test"), "test" , new TinyServiceOptions()))
            {
                var client = TinyClient.Create<IRoot>("test");

                Console.WriteLine(client.Environment);

                var clone = client.Clone();
                Console.WriteLine(clone.Environment);

                Console.WriteLine(clone.Combine("_"));

                Console.ReadLine();
            }
        }
    }
}
