using System.IO;

namespace Pimix.IO.FileFormats
{
    public abstract class PimixFileFormat
    {
        public FileInformation Info { get; set; }

        public abstract Stream GetEncodeStream(Stream rawStream);

        public abstract Stream GetDecodeStream(Stream encodedStream);
    }
}
