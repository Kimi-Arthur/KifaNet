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
            RemoveMissingFiles();
            DeduplicateFiles();

            return 0;
        }

        void RemoveMissingFiles() {
            var (_, files) = PimixFile.ExpandLogicalFiles(FileNames, fullFile: true);
            var filesToRemove = files.Where(file => file.HasEntry && !file.Exists()).ToList();

            if (filesToRemove.Count == 0) {
                logger.Info("No missing files found.");
                return;
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
        }

        void DeduplicateFiles() {
            var (_, files) = PimixFile.ExpandFiles(FileNames, fullFile: true);
            foreach (var sameFiles in files.GroupBy(f => $"{f.Host}/{f.FileInfo.Sha256}")) {
                var target = sameFiles.First();
                foreach (var file in sameFiles.Skip(1)) {
                    logger.Info($"Removing {file} and linking it to {target}...");
                    file.Delete();
                    file.Unregister();
                    target.Copy(file);
                    file.Add();
                    logger.Info($"Linked {file} to {target}.");
                }
            }
        }
    }
}
