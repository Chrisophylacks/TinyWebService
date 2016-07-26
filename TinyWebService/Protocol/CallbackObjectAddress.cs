namespace TinyWebService.Protocol
{
    internal sealed class CallbackObjectAddress
    {
        public CallbackObjectAddress(string endpoint, string instanceId)
        {
            Endpoint = endpoint;
            InstanceId = instanceId;
        }

        public string Endpoint { get; }

        public string InstanceId { get; }

        public static CallbackObjectAddress Parse(string encodedAddress)
        {
            var terms = encodedAddress.Split('~');
            return new CallbackObjectAddress(terms[0], terms[1]);
        }

        public string Encode()
        {
            return Endpoint + "~" + InstanceId;
        }
    }
}