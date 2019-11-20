using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;

namespace Pimix.IO {
    public abstract class StorageClient : IDisposable {
        public virtual void Dispose() {
        }

        public virtual IEnumerable<FileInformation> List(string path, bool recursive = false)
            => Enumerable.Empty<FileInformation>();

        public bool Exists(string path) => Length(path) > 0;

        public abstract long Length(string path);

        public virtual FileInformation QuickInfo(string path) => new FileInformation();

        public abstract void Delete(string path);

        public abstract void Touch(string path);

        public virtual void Copy(string sourcePath, string destinationPath, bool neverLink = false) {
            Write(destinationPath, OpenRead(sourcePath));
        }

        public virtual void Move(string sourcePath, string destinationPath) {
            Copy(sourcePath, destinationPath);
            Delete(sourcePath);
        }

        public abstract Stream OpenRead(string path);

        public abstract void Write(string path, Stream stream);

        public virtual (long total, long used) GetQuota() => (0, 0);
    }
}
