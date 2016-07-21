using System.IO;

namespace Pimix.IO.FileFormats
{
    class RawFileFormat : PimixFileFormat
    {
        public override Stream GetEncodeStream(Stream rawStream)
            => rawStream;

        public override Stream GetDecodeStream(Stream encodedStream)
            => encodedStream;
    }
}
