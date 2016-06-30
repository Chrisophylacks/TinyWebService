using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

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
            return First.Value + separator + Second.Value;
        }

        public IRoot Clone()
        {
            return new Root(Environment + "_clone");
        }

        public string Environment { get; private set; }
    }

    public interface IDispatcherRoot
    {
        Task<IDispatcherRoot> Clone(string text);
        Task EnterFrame();
        void ExitFrame();
    }

    public class DispatcherRoot : DispatcherObject, IDispatcherRoot
    {
        private Stack<DispatcherFrame> _frames = new Stack<DispatcherFrame>();

        public async Task<IDispatcherRoot> Clone(string text)
        {
            return new DispatcherRoot();
        }

        public Task EnterFrame()
        {
            Console.WriteLine("Enter");
            var frame = new DispatcherFrame(true);
            _frames.Push(frame);
            var tcs = new TaskCompletionSource<object>();
            Dispatcher.BeginInvoke(new Action(() => Dispatcher.PushFrame(frame)));
            Dispatcher.BeginInvoke(new Action(() => tcs.SetResult(null)));
            return tcs.Task;
        }

        public void ExitFrame()
        {
            Console.WriteLine("Exit");
            _frames.Pop().Continue = false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var dt = new DispatcherThread())
            {
                dt.Invoke(() => TinyService.Run(new DispatcherRoot(), "test", new TinyServiceOptions()));

                var client = TinyClient.Create<IDispatcherRoot>("test");

                //client.EnterFrame().Wait();
                var clone = client.Clone("a b c").Result;
                //client.ExitFrame();
            }

            Console.ReadLine();
        }
    }

    public sealed class DispatcherThread : IDisposable
    {
        private ManualResetEventSlim _started = new ManualResetEventSlim(false);
        private Thread _thread;

        public DispatcherThread()
        {
            _thread = new Thread(ThreadProc);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Name = "DispatcherThread";
            _thread.Start();
            _started.Wait();
        }

        public void Invoke(Action action)
        {
            Dispatcher.FromThread(_thread).Invoke(action);
        }

        public void Dispose()
        {
            Dispatcher.FromThread(_thread).BeginInvokeShutdown(DispatcherPriority.Normal);
            _thread.Join();
        }

        private void ThreadProc()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => _started.Set()));
            Dispatcher.Run();
        }
    }
}
