using System.IO;

namespace Pimix.IO.FileFormats
{
    public class PimixFile
    {
        public virtual Stream GetEncodeStream(Stream rawStream)
            => rawStream;

        public virtual Stream GetDecodeStream(Stream encodedStream)
            => encodedStream;
    }
}
