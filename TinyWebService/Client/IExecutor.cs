namespace TinyWebService.Client
{
    internal interface IExecutor
    {
        string Execute(string pathAndQuery);
    }
}