using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace Kifa.IO;

public abstract class StorageClient : IDisposable {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public virtual void Dispose() {
    }

    public virtual IEnumerable<FileInformation> List(string path, bool recursive = false)
        => Enumerable.Empty<FileInformation>();

    public bool Exists(string path) => Length(path) > 0;

    public abstract long Length(string path);

    public virtual FileInformation QuickInfo(string path)
        => new() {
            Size = Length(path)
        };

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

    public abstract string Type { get; }

    public abstract string Id { get; }

    public override string ToString() => $"{Type}:{Id}";
}
