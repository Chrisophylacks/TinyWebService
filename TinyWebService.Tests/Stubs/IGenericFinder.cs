namespace TinyWebService.Tests.Stubs
{
    public interface IGenericFinder
    {
        T Find<T>(string key);
        void Test<T>();
        void Find(decimal value);
    }
}