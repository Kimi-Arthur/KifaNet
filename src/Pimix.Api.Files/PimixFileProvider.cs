using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Pimix.Api.Files {
    public class PimixFileProvider : IFileProvider {
        public IFileInfo GetFileInfo(string path) => new PimixFileInfo(new PimixFile(path));

        public IDirectoryContents GetDirectoryContents(string path)
            => new NotFoundDirectoryContents();

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    class PimixFileInfo : IFileInfo {
        readonly PimixFile file;

        public PimixFileInfo(PimixFile pimixFile) {
            file = pimixFile;
        }

        public Stream CreateReadStream() => file.OpenRead();

        public bool Exists => file.Exists();
        public long Length => file.FileInfo.Size.GetValueOrDefault();
        public string PhysicalPath => null;
        public string Name => file.BaseName;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => file.Exists();
    }
}
