using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("upload", HelpText = "Upload file to a cloud location.")]
    class UploadCommand : FileUtilCommand {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var source = new PimixFile(FileUri);

            if (!source.Exists()) {
                logger.Error("Source {0} doesn't exist", FileUri);
                return 1;
            }

            var sourceCheckResult = source.Add();

            if (sourceCheckResult.infoDiff != FileProperties.None) {
                logger.Error("Source is wrong! The following fields differ: {0}", sourceCheckResult.infoDiff);
                return 1;
            }

            var destinationLocation = FileInformation.CreateLocation(source.Id);
            var destination = new PimixFile(destinationLocation);

            if (destination.Exists()) {
                var destinationCheckResult = destination.Add();

                if (destinationCheckResult.infoDiff == FileProperties.None) {
                    logger.Info("Already uploaded!");
                    return 0;
                } else {
                    logger.Warn("Destination exists, but doesn't match.");
                    return 2;
                }
            }

            source.Copy(destination);

            if (destination.Exists()) {
                var destinationCheckResult = destination.Add();
                if (destinationCheckResult.infoDiff == FileProperties.None) {
                    logger.Info("Successfully uploaded {0} to {1}!", source, destination);
                    return 0;
                } else {
                    destination.Delete();
                    logger.Error(
                        "Upload failed! The following fields differ (removed): {0}",
                        destinationCheckResult.infoDiff
                    );
                    return 2;
                }
            } else {
                logger.Fatal("Destination doesn't exist unexpectedly!");
                return 2;
            }
        }
    }
}