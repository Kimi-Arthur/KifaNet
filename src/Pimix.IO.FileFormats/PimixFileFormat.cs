using System;
using System.IO;

namespace Pimix.IO.FileFormats {
    public class PimixFileFormat {
        public virtual Stream GetEncodeStream(Stream rawStream, FileInformation info)
            => throw new NotImplementedException();

        public virtual Stream GetDecodeStream(Stream encodedStream, string encryptionKey = null)
            => throw new NotImplementedException();
    }
}
