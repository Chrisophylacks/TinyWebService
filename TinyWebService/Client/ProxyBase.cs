using System.Collections.Generic;
using System.Threading.Tasks;
using TinyWebService.Infrastructure;
using TinyWebService.Protocol;

namespace TinyWebService.Client
{
    internal abstract class ProxyBase
    {
        private readonly IExecutor _executor;
        protected readonly IEndpoint Endpoint;

        protected ProxyBase(IEndpoint endpoint, string address)
        {
            Endpoint = endpoint;
            Address = ObjectAddress.Parse(address);
            _executor = Endpoint.GetExecutor(Address.Endpoint + "/");
        }

        public ObjectAddress Address { get; }

        public Task<T> CastProxy<T>()
            where T : class
        {
            return ExecuteQueryRet<T>(TinyProtocol.DetachCommand, CreateQueryBuilder());
        }

        protected Task ExecuteQuery(string subPath, IDictionary<string, string> parameters)
        {
            return _executor.Execute(subPath, parameters);
        }

        protected Task<T> ExecuteQueryRet<T>(string subPath, IDictionary<string, string> parameters)
        {
            return _executor.Execute(subPath, parameters).ContinueWith(x => TinyProtocol.Serializer<T>.Deserialize(Endpoint, x.Result), TaskContinuationOptions.ExecuteSynchronously);
        }

        protected T CreateMemberProxy<T>(string subPath)
            where T : class
        {
            return TinyProtocol.Serializer<T>.Deserialize(Endpoint, Address.Combine(subPath).Encode());
        }

        protected IDictionary<string, string> CreateQueryBuilder()
        {
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Address.InstanceId))
            {
                dict[TinyProtocol.InstanceIdParameterName] = Address.InstanceId;
            }
            return dict;
        }
    }
}
