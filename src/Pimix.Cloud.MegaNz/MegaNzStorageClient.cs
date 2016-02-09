using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pimix.IO;

namespace Pimix.Cloud.MegaNz
{
    public class MegaNzStorageClient : StorageClient
    {
        public override void Copy(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string path)
        {
            throw new NotImplementedException();
        }

        public override bool Exists(string path)
        {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string path)
        {
            throw new NotImplementedException();
        }

        public override void Write(string path, Stream stream = null, FileInformation fileInformation = null, bool match = true)
        {
            throw new NotImplementedException();
        }
    }
}
