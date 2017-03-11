using System;
using System.Threading;

namespace TinyWebService.Utilities
{
    internal sealed class SimpleTimer : ITimer
    {
        private readonly TimeSpan _interval;
        private readonly Timer _timer;

        public SimpleTimer(TimeSpan interval)
        {
            _interval = interval;
            if (interval != TimeSpan.Zero)
            {
                _timer = new Timer(TimerCallback, null, interval, interval);
            }
        }

        public event Action Tick;

        public TimeSpan Uptime { get; private set; }
        public TimeSpan NextUptime => Uptime + _interval;

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void TimerCallback(object state)
        {
            Uptime += _interval;
            Tick?.Invoke();
        }
    }
}