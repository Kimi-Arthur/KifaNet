using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommandLine;
using Pimix.Api.Files;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("clean", HelpText = "Clean subtitle file.")]
    class CleanCommand : PimixFileCommand {
        public override Func<List<PimixFile>, string> InstanceConfirmText
            => files => $"Confirm cleaning comments for the {files.Count} files above?";

        public override int ExecuteOneInstance(PimixFile file) {
            var lines = new List<string>();
            using (var sr = new StreamReader(file.OpenRead())) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    lines.Add(line);
                }
            }

            file.Delete();
            file.Write(
                new MemoryStream(
                    new UTF8Encoding(false).GetBytes(string.Join("\n", lines) + "\n")));
            return 0;
        }
    }
}
