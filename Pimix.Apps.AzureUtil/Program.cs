using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Pimix.Apps.AzureUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            var b = new CloudPageBlob(new Uri("blob"), new StorageCredentials("user", "pass"));
            b.FetchAttributes();
            b.ClearPages(0, b.Properties.Length);
        }
    }
}
