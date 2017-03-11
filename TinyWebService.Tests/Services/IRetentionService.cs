namespace TinyWebService.Tests.Services
{
    public interface IRetentionService
    {
        IRetentionInstance GetInstance(string text);
    }

    public interface IRetentionInstance
    {
        string Call();
    }
}