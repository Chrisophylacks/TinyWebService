using System;

namespace TinyWebService.Utilities
{
    internal interface ITimer : IDisposable
    {
        TimeSpan Uptime { get; }
        TimeSpan NextUptime { get; }
        event Action Tick;
    }
}