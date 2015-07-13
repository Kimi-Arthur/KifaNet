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
            return new InfoCommand() { FileUri = FileUri, VerifyAll = true }.Execute();
        }
    }
}
