using System;
using CommandLine;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("ls", HelpText = "List files and folders in the FOLDER.")]
    class ListCommand : PimixFileCommand {
        [Option('l', "long", HelpText = "Long list mode")]
        public bool LongListMode { get; set; } = false;

        int counter;

        public override int Execute() {
            var result = base.Execute();
            Console.WriteLine($"\nIn total, {counter} files in {string.Join(", ", FileNames)}");
            return result;
        }

        protected override int ExecuteOne(string file) {
            counter++;
            Console.WriteLine(file);
            return 0;
        }

        protected override int ExecuteOneInstance(PimixFile file) {
            counter++;
            Console.WriteLine(LongListMode ? $"{file}\t{file.FileInfo.Size}" : file.ToString());
            return 0;
        }
    }
}
