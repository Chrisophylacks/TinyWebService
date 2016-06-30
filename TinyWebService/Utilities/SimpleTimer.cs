﻿using System;
using System.Threading;

namespace TinyWebService.Utilities
{
    internal sealed class SimpleTimer : ITimer
    {
        private readonly Timer _timer;

        public SimpleTimer(TimeSpan interval)
        {
            if (interval != TimeSpan.Zero)
            {
                _timer = new Timer(TimerCallback, null, interval, interval);
            }
        }

        public event Action Tick;

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void TimerCallback(object state)
        {
            Tick?.Invoke();
        }
    }
}