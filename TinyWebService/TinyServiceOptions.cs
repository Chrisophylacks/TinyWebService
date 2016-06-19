using System;

namespace TinyWebService
{
    public sealed class TinyServiceOptions
    {
        public const int DefaultPort = 14048;

        public TinyServiceOptions()
        {
            Port = DefaultPort;
            CleanupInterval = TimeSpan.FromMinutes(5);
        }

        public int Port { get; set; }

        public bool AllowExternalConnections { get; set; }

        public TimeSpan CleanupInterval { get; set; }
    }
}