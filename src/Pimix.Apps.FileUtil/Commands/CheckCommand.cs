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

        protected override int ExecuteOneInstance(PimixFile file) {
            file = new PimixFile(file.ToString());
            logger.Info($"Checking {file} in {(QuickCheck ? "quick" : "full")} mode...");
            var info = new FileInformation();
            if (QuickCheck) {
                info.AddProperties(file.OpenRead(), FileProperties.SliceMd5);
                var compareResults = info.CompareProperties(file.FileInfo, FileProperties.AllVerifiable);
                if (compareResults != FileProperties.None) {
                    logger.Error($"Quick check failed for {file} ({compareResults}).");
                    throw new Exception($"Quick check failed ({compareResults}).");
                }

                logger.Info($"Quick check passed for {file}");
            } else {
                info.AddProperties(file.OpenRead(), FileProperties.AllVerifiable);
                var compareResults = info.CompareProperties(file.FileInfo, FileProperties.AllVerifiable);
                if (compareResults != FileProperties.None) {
                    logger.Error($"Full check failed for {file} ({compareResults}).");
                    throw new Exception($"Full check failed ({compareResults}).");
                }

                file.Register(true);

                logger.Info($"Full check passed for {file}");
            }

            return 0;
        }
    }
}
