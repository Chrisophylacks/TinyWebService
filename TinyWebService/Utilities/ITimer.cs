using System;

namespace TinyWebService.Utilities
{
    internal interface ITimer : IDisposable
    {
        event Action Tick;
    }
}