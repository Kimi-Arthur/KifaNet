using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Kifa.Api.Files;

public class KifaFileProvider : IFileProvider {
    public IFileInfo GetFileInfo(string path) => new KifaFileInfo(new KifaFile(id: path));

    public IDirectoryContents GetDirectoryContents(string path) => new NotFoundDirectoryContents();

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
}

class KifaFileInfo : IFileInfo {
    readonly KifaFile file;

    public KifaFileInfo(KifaFile file) {
        this.file = file;
    }

    public Stream CreateReadStream() => file.OpenRead();

    public bool Exists => file.Exists();
    public long Length => file.Length;
    public string? PhysicalPath => null;
    public string Name => file.BaseName;

    public DateTimeOffset LastModified => DateTimeOffset.Parse("2010-11-25 00:00:00Z");

    public bool IsDirectory => file.Exists();
}
