using System;
using System.Text;
using CommandLine;
using Pimix.Api.Files;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("generate", HelpText = "Generate subtitle.")]
    class GenerateCommand : SubUtilCommand {
        [Value(0, Required = true, HelpText = "Target file to generate subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            foreach (var file in target.Parent.List(ignoreFiles: false,
                pattern: $"{target.BaseName.Normalize(NormalizationForm.FormD)}.*")) {
                Console.WriteLine(file);
            }

            return 0;
        }
    }
}
