using System.IO;

namespace Pimix.IO.FileFormats
{
    public class RawFileFormat : PimixFileFormat
    {
        public static PimixFileFormat Get(string fileSpec)
        {
            return new RawFileFormat();
        }

        public override string ToString()
            => null;

        public override Stream GetEncodeStream(Stream rawStream)
            => rawStream;

        public override Stream GetDecodeStream(Stream encodedStream)
            => encodedStream;
    }
}
