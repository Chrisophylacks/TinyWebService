using System;

namespace TinyWebService.Infrastructure
{
    internal sealed class ObjectAddress
    {
        public ObjectAddress(string endpoint, string instanceId)
        {
            Endpoint = endpoint;
            InstanceId = instanceId;
        }

        public string Endpoint { get; private set; }

        public string InstanceId { get; private set; }

        public static ObjectAddress Parse(string encodedAddress)
        {
            var terms = encodedAddress.Split('~');
            if (terms.Length == 1)
            {
                return new ObjectAddress(terms[0], null);
            }
            return new ObjectAddress(terms[0], terms[1]);
        }

        public ObjectAddress Combine(string subPath)
        {
            return new ObjectAddress(Endpoint + "/" + subPath, InstanceId);
        }

        public string Encode()
        {
            if (String.IsNullOrEmpty(InstanceId))
            {
                return Endpoint;
            }

            return Endpoint + "~" + InstanceId;
        }
    }
}