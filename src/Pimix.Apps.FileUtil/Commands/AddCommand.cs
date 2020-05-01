using System;
using System.Collections.Generic;
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

            var executionHandler = new PimixExecutionHandler<PimixFile>(logger);

            foreach (var file in files) {
                executionHandler.Execute(file, AddFile, "Failed to add {0}.");
            }

            return executionHandler.PrintSummary("Failed to add the following {0} files:");
        }

        void AddFile(PimixFile file) {
            logger.Info($"Adding {file}...");
            var result = file.Add(ForceRecheck);

            if (result == FileProperties.None) {
                logger.Info($"Successfully added {file}");
                return;
            }

            if (!Overwrite) {
                throw new PimixExecutionException($"Conflict with recorded file info! Please check: {result}");
            }

            var info = file.CalculateInfo(FileProperties.AllVerifiable);
            Console.WriteLine($"{info}\nConfirm overwriting with new data?");
            Console.ReadLine();
            FileInformation.Client.Update(info);
            file.Register(true);
            logger.Info($"Successfully updated data and added {file}.");
        }
    }
}
