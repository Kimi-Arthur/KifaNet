using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    }
}
