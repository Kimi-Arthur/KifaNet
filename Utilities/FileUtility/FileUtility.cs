using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HashLib;

namespace Pimix.Storage
{
    public static class FileUtility
    {
        public static FileInformation GetInformation(Stream stream, FileProperties requiredProperties)
        {
            FileInformation info = new FileInformation();

            if (requiredProperties.HasFlag(FileProperties.Size)
                || requiredProperties.HasFlag(FileProperties.BlockSize)
                || (requiredProperties & FileProperties.AllBlockHashes) != FileProperties.None
                || (requiredProperties & FileProperties.AllHashes) != FileProperties.None)
            {
                info.Size = stream.Length;
            }

            if (requiredProperties.HasFlag(FileProperties.BlockSize)
                || (requiredProperties & FileProperties.AllBlockHashes) != FileProperties.None)
            {
                info.BlockSize = GetBlockSize(info.Size.Value);
            }

            if (requiredProperties.HasFlag(FileProperties.SliceMD5))
            {
                byte[] buffer = new byte[SliceLength];
                stream.Seek(0, SeekOrigin.Begin);
                info.SliceMD5 = (stream.Read(buffer, 0, SliceLength) == SliceLength)
                    ? new MD5CryptoServiceProvider().ComputeHash(buffer, 0, SliceLength).Dump()
                    : null;
            }

            if ((requiredProperties & FileProperties.AllHashes) != FileProperties.None)
            {
                List<HashAlgorithm> hashers = new List<HashAlgorithm>
                {
                    requiredProperties.HasFlag(FileProperties.MD5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.SHA1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.SHA256) ? new SHA256CryptoServiceProvider() : null
                };

                info.BlockMD5 = requiredProperties.HasFlag(FileProperties.BlockMD5) ? new List<string>() : null;
                info.BlockSHA1 = requiredProperties.HasFlag(FileProperties.BlockSHA1) ? new List<string>() : null;
                info.BlockSHA256 = requiredProperties.HasFlag(FileProperties.BlockSHA256) ? new List<string>() : null;

                List<HashAlgorithm> blockHashers = new List<HashAlgorithm>
                {
                    requiredProperties.HasFlag(FileProperties.BlockMD5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.BlockSHA1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.BlockSHA256) ? new SHA256CryptoServiceProvider() : null
                };

                stream.Seek(0, SeekOrigin.Begin);
                int blockSize = info.BlockSize ?? GetBlockSize(stream.Length);
                int readLength;
                byte[] buffer = new byte[blockSize];

                var crc32 = requiredProperties.HasFlag(FileProperties.CRC32)
                    ? HashFactory.Checksum.CreateAdler32() : null;
                crc32?.Initialize();

                while ((readLength = stream.Read(buffer, 0, blockSize)) != 0)
                {
                    foreach (var hasher in hashers)
                    {
                        hasher?.TransformBlock(buffer, 0, readLength, buffer, 0);
                    }

                    crc32?.TransformBytes(buffer, 0, readLength);

                    info.BlockMD5?.Add(blockHashers[0].ComputeHash(buffer, 0, readLength).Dump());
                    info.BlockSHA1?.Add(blockHashers[1].ComputeHash(buffer, 0, readLength).Dump());
                    info.BlockSHA256?.Add(blockHashers[2].ComputeHash(buffer, 0, readLength).Dump());
                }

                foreach (var hasher in hashers)
                {
                    hasher?.TransformFinalBlock(buffer, 0, 0);
                }

                info.MD5 = hashers[0]?.Hash.Dump();
                info.SHA1 = hashers[1]?.Hash.Dump();
                info.SHA256 = hashers[2]?.Hash.Dump();
                info.CRC32 = crc32?.TransformFinal().GetBytes().Reverse().ToArray().Dump();
            }

            return info;
        }

        public static FileInformation GetInformation(string path, FileProperties requiredProperties)
        {
            FileInformation info = GetInformation(File.OpenRead(path), requiredProperties);

            if (requiredProperties.HasFlag(FileProperties.Path))
            {
                info.Path = Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, '/');
            }

            return info;
        }

        public static FileInformation GetInformation(string basePath, string path, FileProperties requiredProperties)
        {
            FileInformation info = GetInformation(File.OpenRead($"{basePath}/{path}"), requiredProperties);

            if (requiredProperties.HasFlag(FileProperties.Path))
            {
                info.Path = path.Replace(Path.DirectorySeparatorChar, '/');
            }

            return info;
        }

        static int GetBlockSize(long size)
        {
            return 32 << 20;
        }

        static int SliceLength
            => 256 << 10;
    }
}