using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.IO.FileFormats
{
    public abstract class PimixFile
    {
        public FileInformation Info { get; set; }

        public abstract Stream GetEncodeStream(Stream rawStream);

        public abstract Stream GetDecodeStream(Stream encodedStream);
    }
}
