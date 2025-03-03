using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kifa.IO;

public interface CanCreateStorageClient {
    public static abstract StorageClient Create(string spec);
}

public abstract class StorageClient : IDisposable {
    public virtual void Dispose() {
    }

    public virtual IEnumerable<FileInformation> List(string path, bool recursive = false) => [];

    public bool Exists(string path, long expectedLength = -1) {
        try {
            return expectedLength == 0 ? Length(path) >= 0 : Length(path) > 0;
        } catch (FileNotFoundException) {
            return false;
        }
    }

    public abstract long Length(string path);

    public abstract void Delete(string path);

    public abstract void Touch(string path);

    public virtual void Copy(string sourcePath, string destinationPath, bool neverLink = false) {
        using var stream = OpenRead(sourcePath);
        Write(destinationPath, stream);
    }

    // No need to check existence status.
    public virtual void Move(string sourcePath, string destinationPath) {
        Copy(sourcePath, destinationPath);
        Delete(sourcePath);
    }

    public abstract Stream OpenRead(string path);

    public abstract void Write(string path, Stream stream);

    public virtual FileIdInfo? GetFileIdInfo(string path) => null;

    public abstract string Type { get; }

    public abstract string Id { get; }

    public override string ToString() => $"{Type}:{Id}";
}
