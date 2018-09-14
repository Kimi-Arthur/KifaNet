using System;
using CommandLine;
using Pimix.Service;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("generate", HelpText = "Generate subtitle.")]
    class GenerateCommand : SubUtilCommand {
        [Value(0, Required = true, HelpText = "Target file to generate subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            Console.WriteLine(PimixService.PimixServerApiAddress);

            return 0;
        }
    }
}
