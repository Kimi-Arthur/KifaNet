using System.IO;

namespace Pimix.IO
{
    public abstract class StorageClient
    {
        public abstract bool Exists(string path);

        public virtual FileInformation QuickInfo(string path) => new FileInformation();

        public abstract void Delete(string path);

        public virtual void Copy(string sourcePath, string destinationPath)
        {
            Write(destinationPath, OpenRead(sourcePath));
        }

        public virtual void Move(string sourcePath, string destinationPath)
        {
            Copy(sourcePath, destinationPath);
            Delete(sourcePath);
        }

        public abstract Stream OpenRead(string path);

        public abstract void Write(string path, Stream stream);
    }
}
