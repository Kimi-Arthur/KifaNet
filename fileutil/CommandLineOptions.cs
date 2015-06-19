using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace fileutil
{
    class CommandLineOptions
    {
        public CopyCommandOptions CopyCommand { get; set; }
    }

    [Verb("cp", HelpText = "Copy file from SOURCE to DEST.")]
    class CopyCommandOptions
    {
        [Value(0, Required = true)]
        public string SourceUri { get; set; }

        [Value(1, Required = true)]
        public string DestinationUri { get; set; }
    }
}
