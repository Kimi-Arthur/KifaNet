using System.IO;

namespace Pimix.IO.FileFormats
{
    public abstract class PimixFileFormat
    {
        public abstract Stream GetEncodeStream(Stream rawStream, FileInformation info);

        public abstract Stream GetDecodeStream(Stream encodedStream, FileInformation info);
    }
}
