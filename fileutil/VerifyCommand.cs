using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Pimix.IO;

namespace fileutil
{
    [Verb("verify", HelpText = "Verify the file is in compliant with the data stored in server.")]
    class VerifyCommand : Command
    {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        public override int Execute()
        {
            Uri uri = new Uri(FileUri);

            using (var stream = Helpers.GetDataStream(FileUri))
            {
                var infoFromServer = FileInformation.Get(uri.LocalPath);
                var infoGenerated = FileInformation.GetInformation(stream, infoFromServer.GetProperties());

                // We will first manual test some fields for simplicity.
                if (infoFromServer.Size != infoGenerated.Size)
                {
                    Console.WriteLine("Size Differ");

                    return 1;
                }

                if (infoFromServer.SHA256 != infoGenerated.SHA256)
                {
                    Console.WriteLine("SHA256 Differ");

                    return 1;
                }
            }

            return 0;
        }
    }
}
