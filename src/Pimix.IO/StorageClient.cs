using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pimix.IO {
    public abstract class StorageClient {
        public virtual IEnumerable<FileInformation> List(string path, bool recursive = false)
            => Enumerable.Empty<FileInformation>();

        public abstract bool Exists(string path);

        public virtual FileInformation QuickInfo(string path) => new FileInformation();

        public abstract void Delete(string path);

        public virtual void Copy(string sourcePath, string destinationPath) {
            Write(destinationPath, OpenRead(sourcePath));
        }

        public virtual void Move(string sourcePath, string destinationPath) {
            Copy(sourcePath, destinationPath);
            Delete(sourcePath);
        }

        public abstract Stream OpenRead(string path);

        public abstract void Write(string path, Stream stream);

        public virtual string GetPath(string path) {
            throw new System.NotImplementedException();
        }
    }
}
