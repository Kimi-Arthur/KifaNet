using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Pimix.IO;

namespace fileutil
{
    [Verb("ln", HelpText = "Create a link to TARGET with the name LINK_NAME.")]
    class LinkCommand : FileUtilCommand
    {
        [Value(0, MetaName = "TARGET", MetaValue = "STRING", Required = true, HelpText = "The target for this link.")]
        public string Target { get; set; }

        [Value(1, MetaName = "LINK_NAME", MetaValue = "STRING", Required = true, HelpText = "The link's name.")]
        public string LinkName { get; set; }

        public override int Execute()
        {
            if (!Target.StartsWith("/") || !LinkName.StartsWith("/"))
            {
                Console.Error.WriteLine("You should use absolute file path for the two arguments.");
                return 1;
            }

            return FileInformation.Post(new FileInformation { Id = Target, Path = LinkName }, LinkName) ? 0 : 1;
        }
    }
}
