using System;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Subtitle.Srt;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("generate", HelpText = "Generate subtitle.")]
    class GenerateCommand : SubUtilCommand {
        [Value(0, Required = true, HelpText = "Target file to generate subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            foreach (var file in target.Parent.List(ignoreFiles: false,
                pattern: $"{target.BaseName.Normalize(NormalizationForm.FormD)}.??.srt")) {
                Console.WriteLine($"Subtitle: {file}");
                using (var sr = new StreamReader(file.OpenRead())) {
                    var s = SrtDocument.Parse(sr.ReadToEnd());
                    foreach (var line in s.Lines) {
                        Console.WriteLine(line.ToAss().GenerateAssText());
                    }
                }
            }

            return 0;
        }
    }
}
