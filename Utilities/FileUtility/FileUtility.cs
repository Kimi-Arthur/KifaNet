using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Storage
{
    public static class FileUtility
    {
        public static FileInformation GetInformation(Stream stream, FileProperties requiredProperties)
        {
            FileInformation info = new FileInformation();

            if (requiredProperties.HasFlag(FileProperties.Size))
            {
                info.Size = stream.Length;
            }

            if ((requiredProperties | FileProperties.AllHashes) != FileProperties.None)
            {
                List<HashAlgorithm> hashers = new List<HashAlgorithm>
                {
                    requiredProperties.HasFlag(FileProperties.MD5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.SHA1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.SHA256) ? new SHA256CryptoServiceProvider() : null
                };

                stream.Seek(0, SeekOrigin.Begin);
                int blockSize = 32 << 20, len;
                byte[] buffer = new byte[blockSize];
                while ((len = stream.Read(buffer, 0, blockSize)) != 0)
                {
                    foreach (var hasher in hashers)
                    {
                        hasher?.TransformBlock(buffer, 0, len, buffer, 0);
                    }
                }

                foreach (var hasher in hashers)
                {
                    hasher?.TransformFinalBlock(buffer, 0, 0);
                }

                info.MD5 = hashers[0]?.Hash.Dump();
                info.SHA1 = hashers[1]?.Hash.Dump();
                info.SHA256 = hashers[2]?.Hash.Dump();
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
    }
}