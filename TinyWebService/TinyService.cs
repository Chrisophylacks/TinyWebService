using System;
using TinyWebService.Service;

namespace TinyWebService
{
    public static class TinyService
    {
        public static IDisposable Run<T>(T service, string name, int port = TinyHttpServer.DefaultPort)
            where T : class
        {
            var dispatcher = new SimpleDispatcher<T>(service);
            return new TinyHttpServer(name, port, () => new Session(dispatcher));
        }
    }
}