using System;
using System.Collections.Generic;

namespace TinyWebService.Experiments
{
    public interface ICallback
    {
        void Call(string value);
    }

    public interface IServer
    {
        void Register(ICallback callback);
        void Raise(string value);
    }

    public sealed class Server : IServer
    {
        private readonly List<ICallback> _callbacks = new List<ICallback>();

        public void Register(ICallback callback)
        {
            callback.Call("accepted");
            _callbacks.Add(callback);
        }

        public void Raise(string value)
        {
            foreach (var callback in _callbacks)
            {
                callback.Call(value);
            }
        }
    }

    class Callback : ICallback
    {
        public void Call(string value)
        {
            Console.WriteLine(value);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TinyClient.EnableDuplexMode();

            using (TinyService.Run(new Server(), "test", new TinyServiceOptions()))
            {
                var client = TinyClient.Create<IServer>("test");
                client.Register(new Callback());
                client.Raise("x");
                client.Register(new Callback());
                client.Raise("y");
            }

            Console.ReadLine();
        }
    }
}
