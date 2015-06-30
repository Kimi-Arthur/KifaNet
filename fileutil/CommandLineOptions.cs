using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace fileutil
{
    [Verb("upload", HelpText = "Upload file baidu cloud.")]
    class UploadCommandOptions : Command
    {
        [Value(0, Required = true)]
        public string SourceUri { get; set; }

        [Value(1, Required = true)]
        public string DestinationUri { get; set; }

        [Option('c', "chunk-size", HelpText = "The chunk size used to copy data")]
        public string ChunkSize { get; set; } = ConfigurationManager.AppSettings["BufferSize"];
    }

    [Verb("info", HelpText = "Generate information of the specified file.")]
    class InfoCommandOptions : Command
    {
        [Value(0, Required = true)]
        public string FileUri { get; set; }
    }
}
