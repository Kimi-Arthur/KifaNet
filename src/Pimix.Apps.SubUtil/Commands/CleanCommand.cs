using System.Collections.Generic;
using System.IO;
using System.Text;
using CommandLine;
using Pimix.Api.Files;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("clean", HelpText = "Clean subtitle file.")]
    class CleanCommand : PimixCommand {
        [Value(0, Required = true, HelpText = "Target file to normalize subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var lines = new List<string>();
            using (var sr = new StreamReader(target.OpenRead())) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    lines.Add(line);
                }
            }

            target.Delete();
            target.Write(
                new MemoryStream(new UTF8Encoding(false).GetBytes(string.Join("\n", lines))));
            return 0;
        }
    }
}
