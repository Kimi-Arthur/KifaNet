using System;

namespace Pimix.IO
{
    [Flags]
    public enum FileProperties
    {
        None = 0x0,
        Id = 0x1,
        Path = 0x1,
        Size = 0x2,
        BlockSize = 0x4,
        MD5 = 0x100,
        SHA1 = 0x200,
        SHA256 = 0x400,
        CRC32 = 0x800,
        BlockMD5 = 0x1000,
        BlockSHA1 = 0x2000,
        BlockSHA256 = 0x4000,
        SliceMD5 = 0x8000,
        EncryptionKey = 0x10000,
        Basic = Size | BlockSize,
        AllHashes = MD5 | SHA1 | SHA256 | CRC32,
        AllBlockHashes = BlockMD5 | BlockSHA1 | BlockSHA256,
        AllBaiduCloudRapidHashes = Size | MD5 | SliceMD5 | CRC32,
        All = Basic | AllHashes | AllBlockHashes | AllBaiduCloudRapidHashes | EncryptionKey,
        AllVerifiable = Basic | AllHashes | AllBlockHashes | SliceMD5
    }
}
