using System.IO;

namespace Kifa.IO.FileFormats; 

public class RawFileFormat : KifaFileFormat {
    public static readonly RawFileFormat Instance = new RawFileFormat();

    public override string ToString() => "";

    public override Stream GetEncodeStream(Stream rawStream, FileInformation info) => rawStream;

    public override Stream GetDecodeStream(Stream encodedStream, string? encryptionKey = null)
        => encodedStream;
}