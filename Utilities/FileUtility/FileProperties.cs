using System;

namespace Pimix.Storage
{
    [Flags]
    public enum FileProperties
    {
        None = 0x0,
        Path = 0x1,
        Size = 0x2,
        BlockSize = 0x4,
        MD5 = 0x100,
        SHA1 = 0x200,
        SHA256 = 0x400,
        BlockMD5 = 0x1000,
        BlockSHA1 = 0x2000,
        BlockSHA256 = 0x4000,
        Basic = Path | Size | BlockSize,
        AllHashes = MD5 | SHA1 | SHA256,
        AllBlockHashes = BlockMD5 | BlockSHA1 | BlockSHA256,
        All = Basic | AllHashes | AllBlockHashes
    }
}
