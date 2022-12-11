using System;
using System.IO;

namespace Kifa.IO.FileFormats;

public abstract class KifaFileFormat {
    public abstract long HeaderSize { get; }

    public abstract Stream GetEncodeStream(Stream rawStream, FileInformation info);

    public abstract Stream GetDecodeStream(Stream encodedStream, string? encryptionKey = null);
}
