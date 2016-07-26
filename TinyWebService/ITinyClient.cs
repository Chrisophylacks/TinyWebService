using System;

namespace TinyWebService
{
    public interface ITinyClient<out T> : IDisposable
    {
        T Client { get; }
    }
}