using System.Runtime.Serialization;

namespace TinyWebService.Tests.Stubs
{
    [DataContract]
    public sealed class DataObject
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public DataObject Nested { get; set; }
    }
}