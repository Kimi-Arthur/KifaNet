using System;
using CommandLine;
using NLog;
using Kifa.Api.Files;
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

        protected override int ExecuteOnePimixFile(KifaFile file) {
            file = new KifaFile(file.ToString());
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

                FileInformation info;
                try {
                    info = file.CalculateInfo(FileProperties.SliceMd5 | FileProperties.Size);
                } catch (Exception e) {
                    logger.Error(e, $"Quick check failed for {file}.");
                    throw new Exception($"Quick check failed for {file}.", e);
                }
 
                var compareResults = info.CompareProperties(file.FileInfo, FileProperties.AllVerifiable);
                if (compareResults != FileProperties.None) {
                    logger.Error($"Quick check failed for {file} ({compareResults}).");
                    throw new Exception($"Quick check failed ({compareResults}).");
                }

                logger.Info($"Quick check passed for {file}");
            } else {
                var alreadyRegistered = file.Registered;
                FileProperties compareResults;
                try {
                    compareResults = file.Add(!SkipKnown);
                } catch (Exception e) {
                    logger.Error(e, $"Full check failed for {file}.");
                    throw new Exception($"Full check failed for {file}.", e);
                }

                if (compareResults != FileProperties.None) {
                    logger.Error($"Full check failed for {file} ({compareResults}).");

                    if (Overwrite) {
                        FileInformation info;
                        try {
                            info = file.CalculateInfo(FileProperties.AllVerifiable);
                        } catch (Exception e) {
                            logger.Error(e, $"Full check failed for {file}.");
                            throw new Exception($"Full check failed for {file}.", e);
                        }
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
