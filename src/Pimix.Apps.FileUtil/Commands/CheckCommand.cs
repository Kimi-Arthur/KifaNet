using System;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("check", HelpText = "Check file integrity.")]
    class CheckCommand : PimixFileCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Option('q', "quick", HelpText =
            "Quick check by only verifying the first block.")]
        public bool QuickCheck { get; set; } = false;

        [Option('s', "skip-known", HelpText =
            "Skip check of file if it's already known.")]
        public bool SkipKnown { get; set; } = false;

        [Option('o', "overwrite", HelpText =
            "Overwrite existing data if asked (with confirmation).")]
        public bool Overwrite { get; set; } = false;

        protected override int ExecuteOnePimixFile(PimixFile file) {
            file = new PimixFile(file.ToString());
            if (!file.Exists()) {
                logger.Info($"{file} doesn't exist.");
                throw new Exception("Doesn't exist.");
            }

            logger.Info($"Checking {file} in {(QuickCheck ? "quick" : "full")} mode...");
            if (QuickCheck) {
                if (SkipKnown && file.Registered) {
                    logger.Info($"Quick check skipped for {file} as it's already registered.");
                    return 0;
                }

                var info = file.CalculateInfo(FileProperties.SliceMd5);
                var compareResults = info.CompareProperties(file.FileInfo, FileProperties.AllVerifiable);
                if (compareResults != FileProperties.None) {
                    logger.Error($"Quick check failed for {file} ({compareResults}).");
                    throw new Exception($"Quick check failed ({compareResults}).");
                }

                logger.Info($"Quick check passed for {file}");
            } else {
                var alreadyRegistered = file.Registered;
                var compareResults = file.Add(!SkipKnown);
                if (compareResults != FileProperties.None) {
                    logger.Error($"Full check failed for {file} ({compareResults}).");

                    if (Overwrite) {
                        var info = file.CalculateInfo(FileProperties.AllVerifiable);
                        Console.WriteLine($"{info}\nConfirm overwriting with new data?");
                        Console.ReadLine();
                        FileInformation.Client.Update(info);
                        // TODO: Need to invalidate all other locations.
                        file.Register(true);
                        logger.Info("Successfully updated data and added file.");
                        return 0;
                    }

                    throw new Exception($"Full check failed ({compareResults}).");
                }

                if (SkipKnown && alreadyRegistered) {
                    logger.Info($"Full check skipped for {file}.");
                } else {
                    logger.Info($"Full check passed for {file}.");
                }
            }

            return 0;
        }
    }
}
