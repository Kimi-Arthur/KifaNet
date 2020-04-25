using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("add", HelpText = "Add file entry.")]
    class AddCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('f', "force-check", HelpText =
            "Check file integrity even if it is already recorded.")]
        public bool ForceRecheck { get; set; } = false;

        [Option('o', "overwrite", HelpText =
            "Overwrite existing data if asked (with confirmation).")]
        public bool Overwrite { get; set; } = false;

        public override int Execute() {
            var (multi, files) = PimixFile.ExpandFiles(FileNames);
            if (multi) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                Console.Write($"Confirm adding the {files.Count} files above?");
                Console.ReadLine();
            }

            return files.Select(f => AddFile(new PimixFile(f.ToString()))).Max();
        }

        int AddFile(PimixFile f) {
            logger.Info("Adding {0}...", f);
            var result = f.Add(ForceRecheck);

            if (result == FileProperties.None) {
                logger.Info("Successfully added {0}", f);
                return 0;
            }

            if (Overwrite) {
                var info = f.CalculateInfo(FileProperties.AllVerifiable);
                Console.WriteLine($"{info}\nConfirm overwriting with new data?");
                Console.ReadLine();
                FileInformation.Client.Update(info);
                f.Register(true);
                logger.Info("Successfully updated data and added file.");
                return 0;
            }

            logger.Warn("Conflict with recorded file info! Please check: {0}", result);
            return 1;
        }
    }
}