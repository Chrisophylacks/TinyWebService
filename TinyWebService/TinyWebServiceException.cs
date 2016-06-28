using System;
using System.Runtime.Serialization;

namespace TinyWebService
{
    [Serializable]
    public class TinyWebServiceException : Exception
    {
        public TinyWebServiceException()
        {
        }

        public TinyWebServiceException(string message) : base(message)
        {
        }

        public TinyWebServiceException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TinyWebServiceException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}