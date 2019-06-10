using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pimix.IO.FileFormats {
    public class PimixFileFormat {
        public virtual Stream GetEncodeStream(Stream rawStream, FileInformation info)
            => throw new NotImplementedException();

        public virtual List<Stream> GetEncodeStreams(Stream rawStream, FileInformation info) =>
            new List<Stream> {GetEncodeStream(rawStream, info)};

        public virtual Stream GetDecodeStream(Stream encodedStream, string encryptionKey = null)
            => throw new NotImplementedException();

        public virtual Stream GetDecodeStream(List<Stream> encodedStreams, string encryptionKey = null)
            => GetDecodeStream(encodedStreams.First(), encryptionKey);
    }
}
