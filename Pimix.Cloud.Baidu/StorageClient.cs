using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Cloud.Baidu
{
    public class StorageClient
    {
        public static Config Config { get; set; }

        public StorageClient()
        {

        }

        public void DownloadFile(string path, Stream output, long offset = 0, long length = -1)
        {
            // Download code
        }

        public Stream GetDownloadStream(string path)
        {
            // Construct a stream and use the method above. Something like a wrapper.
            return null;
        }
    }
}
