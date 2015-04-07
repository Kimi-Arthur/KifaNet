using System;

namespace Pimix.Storage
{
    [Flags]
    public enum FileProperties
    {
        None = 0x0,
        Path = 0x1,
        Size = 0x2,
        MD5 = 0x100,
        SHA1 = 0x200,
        SHA256 = 0x400,
        Basic = Size | Path,
        AllHashes = MD5 | SHA1 | SHA256,
        All = Basic | AllHashes
    }
}
