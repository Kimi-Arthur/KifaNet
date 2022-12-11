using System.IO;

namespace Kifa.IO.FileFormats;

public class RawFileFormat : KifaFileFormat {
    public static readonly RawFileFormat Instance = new();

    public override string ToString() => "";

    public override long HeaderSize => 0;

    public override Stream GetEncodeStream(Stream rawStream, FileInformation info) => rawStream;

    public override Stream GetDecodeStream(Stream encodedStream, string? encryptionKey = null)
        => encodedStream;
}
