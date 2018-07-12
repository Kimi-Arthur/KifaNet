using System.IO;

namespace Pimix.IO.FileFormats {
    public class RawFileFormat : PimixFileFormat {
        public static PimixFileFormat Get(string fileUri) => new RawFileFormat();

        public override string ToString() => "";

        public override Stream GetEncodeStream(Stream rawStream, FileInformation info) => rawStream;

        public override Stream GetDecodeStream(Stream encodedStream, string encryptionKey = null)
            => encodedStream;
    }
}
