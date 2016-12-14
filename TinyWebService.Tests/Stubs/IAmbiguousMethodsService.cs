using System;

namespace TinyWebService.Tests.Stubs
{
    public interface IAmbiguousMethodsService
    {
        void Execute(string arg);
        void Execute(Func<string> arg);
    }
}