using System;

namespace Kifa.IO; 

[Flags]
public enum FileProperties {
    None = 0x0,
    Id = 0x1,
    Size = 0x2,
    Md5 = 0x100,
    Sha1 = 0x200,
    Sha256 = 0x400,
    Crc32 = 0x800,
    Adler32 = 0x1000,
    BlockMd5 = 0x10000,
    BlockSha1 = 0x20000,
    BlockSha256 = 0x40000,
    SliceMd5 = 0x80000,
    EncryptionKey = 0x100000,
    Locations = 0x200000,
    AllHashes = Md5 | Sha1 | Sha256 | Crc32 | Adler32,
    AllBlockHashes = BlockMd5 | BlockSha1 | BlockSha256,
    AllBaiduCloudRapidHashes = Size | Md5 | SliceMd5 | Adler32,
    All = AllVerifiable | EncryptionKey | Locations,
    AllVerifiable = Size | AllHashes | SliceMd5 | AllBlockHashes
}