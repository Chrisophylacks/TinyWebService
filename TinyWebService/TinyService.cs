﻿using System;
using TinyWebService.Protocol;
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
            var session = new Session(new SimpleTimer(actualOptions.CleanupInterval), new SimpleDispatcher<T>(service));
            return new TinyHttpServer(TinyProtocol.CreatePrefix(actualOptions.AllowExternalConnections ? "*" : null, actualOptions.Port, name), session);
        }
    }
}