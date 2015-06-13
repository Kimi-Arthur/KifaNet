using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.IO.FileFormats
{
    public abstract class PimixFileFormat
    {
        public abstract Stream GetEncodeStream(Stream rawStream, FileInformation fileInformation = null);

        public abstract Stream GetDecodeStream(Stream encodedStream, FileInformation fileInformation = null);
    }
}
