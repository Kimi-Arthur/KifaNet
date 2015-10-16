using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.IO
{
    public abstract class StorageClient
    {
        public abstract bool Exists();

        public abstract void Delete(string path);

        public abstract void Copy(string sourcePath, string destinationPath);

        public virtual void Move(string sourcePath, string destinationPath)
        {
            Copy(sourcePath, destinationPath);
            Delete(sourcePath);
        }

        public abstract Stream OpenRead(string path);

        public abstract void Write(string path, Stream stream = null, FileInformation fileInformation = null, bool match = true);
    }
}
