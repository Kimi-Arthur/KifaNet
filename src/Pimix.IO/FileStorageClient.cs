using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.IO
{
    class FileStorageClient : StorageClient
    {
        const int DefaultBlockSize = 32 << 20;

        public override void Copy(string sourcePath, string destinationPath)
            => File.Copy(sourcePath, destinationPath);

        public override void Delete(string path)
            => File.Delete(path);

        public override void Move(string sourcePath, string destinationPath)
            => File.Move(sourcePath, destinationPath);

        public override bool Exists(string path)
            => File.Exists(path);

        public override Stream OpenRead(string path)
            => File.OpenRead(path);

        public override void Write(string path, Stream stream = null, FileInformation fileInformation = null, bool match = true)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int blockSize = fileInformation?.BlockSize ?? DefaultBlockSize;
            Directory.GetParent(path).Create();
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                stream.CopyTo(fs, blockSize);
            }
        }
    }
}
