using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Games.Files;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("decode", HelpText = "Decode file.")]
    class DecodeCommand : PimixCommand {
        [Value(0, Required = true, HelpText = "Target file(s) to import.")]
        public IEnumerable<string> FileNames { get; set; }

        public override int Execute() {
            foreach (var file in FileNames.SelectMany(path =>
                new PimixFile(path).List())) {
                var target = file.Parent.GetFile(file.Name + ".decoded");
                if (file.Extension == "lzs") {
                    target.Write(LzssFile.Decode(file.OpenRead()));
                }
            }

            return 0;
        }
    }
}
