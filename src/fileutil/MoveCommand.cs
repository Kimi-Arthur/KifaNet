using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace fileutil
{
    [Verb("mv", HelpText = "Move file from SOURCE to DEST.")]
    class MoveCommand : FileUtilCommand
    {
        [Value(0, Required = true)]
        public string SourceUri { get; set; }

        [Value(1, Required = true)]
        public string DestinationUri { get; set; }

        public override int Execute()
        {
            int result = new CopyCommand { SourceUri = SourceUri, DestinationUri = DestinationUri, VerifyAll = true, Update = true }.Execute();
            if (result != 0)
            {
                Console.Error.WriteLine("Copy step failed in move command.");
                return result;
            }

            result = new RemoveCommand { FileUri = SourceUri }.Execute();
            if (result != 0)
            {
                Console.Error.WriteLine("Remove step failed in move command.");
                return result;
            }

            return result;
        }
    }
}
