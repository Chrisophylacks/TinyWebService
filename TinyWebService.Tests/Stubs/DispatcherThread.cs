using System;
using System.Threading;
using System.Windows.Threading;

namespace TinyWebService.Tests.Stubs
{
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

        public T Invoke<T>(Func<T> func)
        {
            return Dispatcher.FromThread(_thread).Invoke(func);
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