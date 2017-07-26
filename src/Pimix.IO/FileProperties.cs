using System;

namespace Pimix.IO
{
    [Flags]
    public enum FileProperties
    {
        None = 0x0,
        Id = 0x1,
        Size = 0x2,
        MD5 = 0x100,
        SHA1 = 0x200,
        SHA256 = 0x400,
        CRC32 = 0x800,
        Adler32 = 0x1000,
        BlockMD5 = 0x10000,
        BlockSHA1 = 0x20000,
        BlockSHA256 = 0x40000,
        SliceMD5 = 0x80000,
        EncryptionKey = 0x100000,
        Locations = 0x200000,
        AllHashes = MD5 | SHA1 | SHA256 | CRC32 | Adler32,
        AllBlockHashes = BlockMD5 | BlockSHA1 | BlockSHA256,
        AllBaiduCloudRapidHashes = Size | MD5 | SliceMD5 | Adler32,
        All = AllVerifiable | EncryptionKey | Locations,
        AllVerifiable = Size | AllHashes | SliceMD5 | AllBlockHashes
    }
}
