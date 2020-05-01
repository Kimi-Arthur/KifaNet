using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("clean", HelpText = "Clean file entries.")]
    class CleanCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
        public IEnumerable<string> FileNames { get; set; }

        public override int Execute() {
            var (_, files) = PimixFile.ExpandLogicalFiles(FileNames);
            var filesToRemove = files.Select(file => new PimixFile(file.ToString()))
                .Where(file => file.HasEntry && !file.Exists()).ToList();

            if (filesToRemove.Count == 0) {
                logger.Info("No missing files found.");
                return 0;
            }

            foreach (var file in filesToRemove) {
                Console.WriteLine(file);
            }

            Console.Write(
                $"The {filesToRemove.Count} files above do not actually exist. Confirm removing them from system?");
            Console.ReadLine();

            foreach (var file in filesToRemove) {
                file.Unregister();
            }

            return 0;
        }
    }
}
