using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace fileutil
{
    [Verb("rm", HelpText = "Remove the FILE. Can be either logic path like: pimix:///Software/... or real path like: pimix://xxx@xxx/....")]
    class RemoveCommand : Command
    {
        [Value(0, MetaName = "FILE", MetaValue = "STRING", Required = true, HelpText = "File to be removed.")]
        public string FileUri { get; set; }

        public override int Execute()
        {
            throw new NotImplementedException();
        }
    }
}
