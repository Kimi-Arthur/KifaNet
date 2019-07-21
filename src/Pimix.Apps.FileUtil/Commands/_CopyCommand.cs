using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("cp", HelpText = "Copy file from SOURCE to DEST.")]
    class _CopyCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true)]
        public string SourceUri { get; set; }

        [Value(1, Required = true)]
        public string DestinationUri { get; set; }

        [Option('f', "overwrite", HelpText = "Overwrite destination.")]
        public bool Overwrite { get; set; } = false;

        [Option('v', "verify", HelpText = "Verify destination.")]
        public bool Verify { get; set; } = false;

        public override int Execute() {
            var source = new PimixFile(SourceUri);
            var destination = new PimixFile(DestinationUri);

            if (!source.Exists()) {
                logger.Error("Source does not exist!");
                return 1;
            }

            var destinationSha256 = destination.FileInfo.Sha256;
            if (destinationSha256 != null && destinationSha256 != source.FileInfo.Sha256) {
                logger.Error("Cannot move file between different entities.");
                return 1;
            }

            if (destination.Exists()) {
                if (Overwrite) {
                    logger.Info("Overwriting existing file.");
                    destination.Delete();
                    FileInformation.Client.RemoveLocation(destination.Id, DestinationUri);
                } else {
                    logger.Error("Destination already exists!");
                    logger.Error("Add -f to overwrite.");
                    return 1;
                }
            }

            source.Copy(destination);

            if (!destination.Exists()) {
                logger.Fatal("Destination doesn't exist unexpectedly!");
                return 2;
            }

            if (Verify) {
                var result = destination.Add(Verify);
                if (result != FileProperties.None) {
                    logger.Fatal("Unexpected copy failure.");
                    return 2;
                }
            }

            FileInformation.Client.AddLocation(destination.Id, DestinationUri);
            logger.Info($"Successfully copied {SourceUri} to {DestinationUri}.");
            return 0;
        }
    }
}
