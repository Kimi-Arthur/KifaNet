using System;
using System.Collections.Generic;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("get", HelpText = "Get files.")]
class GetCommand : KifaFileCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Option('l', "lightweight-only", HelpText = "Only get files that need no download.")]
    public bool LightweightOnly { get; set; } = false;

    public override bool Recursive { get; set; } = true;

    protected override Func<List<KifaFile>, string> KifaFileConfirmText
        => files => $"Confirm getting the {files.Count} files above?";

    protected override bool IterateOverLogicalFiles => true;

    protected override int ExecuteOneKifaFile(KifaFile file) {
        if (file.Exists()) {
            if (file.CalculateInfo(FileProperties.Size).Size != file.FileInfo.Size) {
                logger.Info("Target exists but size is incorrect. Assuming incomplete Get result.");
            } else {
                var targetCheckResult = file.Add();

                if (targetCheckResult == FileProperties.None) {
                    logger.Info("Already got!");
                    return 0;
                }

                logger.Warn("Target exists, but doesn't match.");
                return 2;
            }
        }

        file.Unregister();

        var info = file.FileInfo;

        if (info.Locations == null) {
            logger.Error($"No instance exists for {info.Id}!");
            return 1;
        }

        foreach (var location in info.Locations) {
            if (location.Value != null) {
                var linkSource = new KifaFile(location.Key);
                if (linkSource.Client is FileStorageClient && linkSource.IsCompatible(file) &&
                    linkSource.Exists()) {
                    linkSource.Copy(file);
                    file.Register(true);
                    logger.Info("Got {0} through hard linking to {1}.", file, linkSource);
                    return 0;
                }
            }
        }

        if (LightweightOnly) {
            logger.Warn("Not getting {}, which requires downloading.", file);
            return 1;
        }

        var source = new KifaFile(fileInfo: info);
        source.Copy(file);

        if (file.Exists()) {
            logger.Info("Verifying {0}...", file);
            var destinationCheckResult = file.Add();
            if (destinationCheckResult == FileProperties.None) {
                logger.Info("Successfully got {1} from {0}!", source, file);
                source.Register(true);
                return 0;
            }

            logger.Error("Get failed! The following fields differ (not removed): {0}",
                destinationCheckResult);
            return 2;
        }

        logger.Fatal("Destination doesn't exist unexpectedly!");
        return 2;
    }
}
