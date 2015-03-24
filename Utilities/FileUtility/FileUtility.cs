using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Utilities
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

            if (requiredProperties.HasFlag(FileProperties.MD5))
            {
                using (MD5 hasher = new MD5CryptoServiceProvider())
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] hash = hasher.ComputeHash(stream);
                    info.MD5 = BitConverter.ToString(hash).Replace("-", "");
                }
            }

            if (requiredProperties.HasFlag(FileProperties.SHA1))
            {
                using (SHA1 hasher = new SHA1CryptoServiceProvider())
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] hash = hasher.ComputeHash(stream);
                    info.SHA1 = BitConverter.ToString(hash).Replace("-", "");
                }
            }

            if (requiredProperties.HasFlag(FileProperties.SHA256))
            {
                using (SHA256 hasher = new SHA256CryptoServiceProvider())
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] hash = hasher.ComputeHash(stream);
                    info.SHA256 = BitConverter.ToString(hash).Replace("-", "");
                }
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