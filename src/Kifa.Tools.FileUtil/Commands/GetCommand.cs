using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("get", HelpText = "Get files.")]
class GetCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('l', "lightweight-only", HelpText = "Only get files that need no download.")]
    public bool LightweightOnly { get; set; } = false;

    public override bool Recursive { get; set; } = true;

    protected override Func<List<KifaFile>, string> KifaFileConfirmText
        => files => $"Confirm getting the {files.Count} files above?";

    protected override bool IterateOverLogicalFiles => true;

    protected override int ExecuteOneKifaFile(KifaFile file) {
        try {
            file.Add();
            Logger.Info("Already got!");
            return 0;
        } catch (FileNotFoundException) {
            // File expected to be not found.
        } catch (FileCorruptedException ex) {
            Logger.Error(ex, "Target exists, but doesn't match.");
            return 2;
        }

        file.Unregister();

        var info = file.FileInfo;

        if (info?.Locations == null) {
            Logger.Error($"No instance exists for {file.Id}!");
            return 1;
        }

        foreach (var (location, verifyTime) in info.Locations) {
            if (verifyTime != null) {
                KifaFile linkSource;
                try {
                    linkSource = new KifaFile(location);
                } catch (FileNotFoundException ex) {
                    Logger.Debug(ex, $"{location} is not accessible.");
                    continue;
                }

                if (linkSource.IsLocal && linkSource.IsCompatible(file) && linkSource.Exists()) {
                    linkSource.Copy(file);
                    file.Register(true);
                    Logger.Info($"Got {file} through hard linking to {linkSource}.");
                    return 0;
                }
            }
        }

        if (LightweightOnly) {
            Logger.Warn($"Not getting {file}, which requires downloading.");
            return 1;
        }

        var source = new KifaFile(fileInfo: info);
        source.Copy(file);

        try {
            Logger.Info($"Verifying destination {file}...");
            file.Add();
            source.Register(true);
            Logger.Info($"Successfully got destination {file} from {source}!");
            return 0;
        } catch (IOException ex) {
            Logger.Error(ex, $"Failed to get destination {file}.");
            return 2;
        }
    }
}
