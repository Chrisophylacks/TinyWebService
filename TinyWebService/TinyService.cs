using System;
using TinyWebService.Service;
using TinyWebService.Utilities;

namespace TinyWebService
{
    public static class TinyService
    {
        public static IDisposable Run<T>(T service, string name, TinyServiceOptions options = null)
            where T : class
        {
            var actualOptions = options ?? new TinyServiceOptions();
            var session = new Session(new SimpleDispatcher<T>(service), new SimpleTimer(actualOptions.CleanupInterval));
            return new TinyHttpServer(TinyHttpServer.CreatePrefix(actualOptions.AllowExternalConnections ? "*" : null, actualOptions.Port, name), session);
        }
    }
}