using System;

namespace TinyWebService.Protocol
{
    [Flags]
    public enum TypeFlags
    {
        None = 0x0,
        CanSerialize = 0x1,
        CanDeserialize = 0x2,
        Remotable = 0x4,

        DataType = CanSerialize | CanDeserialize,
        ProxyType = CanSerialize | CanDeserialize | Remotable,
        DispatcherType = CanSerialize | Remotable
    }

    public static class TypeFlagsExtensions
    {
        public static bool CanSerialize(this TypeFlags flags)
        {
            return (flags & TypeFlags.CanSerialize) == TypeFlags.CanSerialize;
        }

        public static bool CanDeserialize(this TypeFlags flags)
        {
            return (flags & TypeFlags.CanDeserialize) == TypeFlags.CanDeserialize;
        }

        public static bool CanBuildProxy(this TypeFlags flags)
        {
            return (flags & (TypeFlags.CanDeserialize | TypeFlags.Remotable)) == (TypeFlags.CanDeserialize | TypeFlags.Remotable);
        }

        public static bool CanBuildDispatcher(this TypeFlags flags)
        {
            return (flags & (TypeFlags.CanSerialize | TypeFlags.Remotable)) == (TypeFlags.CanSerialize | TypeFlags.Remotable);
        }
    }
}