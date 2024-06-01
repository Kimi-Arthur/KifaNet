using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Cloud.Google;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("migrate", HelpText = "Migrate Google Drive files to a new cell and quick check on it.")]
public class MigrateCommand : KifaCommand {
    [Option('d', "dry-run", HelpText = "Only dry run, not actually migrate.")]
    public bool DryRun { get; set; } = false;

    [Option('q', "quiet", HelpText = "Quiet mode, no confirmation is requested.")]
    public bool QuietMode { get; set; } = false;

    public override int Execute() {
        var files = FileInformation.Client.List();
        var processed = new Dictionary<string, string>();
        foreach (var (file, info) in files) {
            if (info.Sha256 == null) {
                Console.WriteLine($"{file}:(3) no sha256");
                continue;
            }

            if (processed.TryGetValue(info.Sha256, out var processedFile)) {
                Console.WriteLine($"{file}:(2) sha256 already processed by {processedFile}.");
                continue;
            }

            Console.WriteLine($"Processing {file}...");

            var targetCell = GoogleDriveStorageClient.DefaultCell;
            var source = $"google:good/$/{info.Sha256}.v1";
            var target = $"google:{targetCell}/$/{info.Sha256}.v1";

            bool? sourceRegistered = info.Locations.TryGetValue(source, out var sourceTime)
                ? sourceTime != null
                : null;
            bool? targetRegistered = info.Locations.TryGetValue(target, out var targetTime)
                ? targetTime != null
                : null;

            if (targetRegistered == true && sourceRegistered == null) {
                Console.WriteLine($"{file}:(1) already moved.");
                processed.Add(info.Sha256, info.Id);
                continue;
            }

            if (sourceRegistered == true && targetRegistered == null) {
                var sourceFound = new KifaFile(source).Exists();
                var targetFound = new KifaFile(target).Exists();

                if (sourceFound && !targetFound) {
                    if (DryRun) {
                        Console.WriteLine($"{file}:(0) to move.");
                    } else if (QuietMode || Confirm($"Confirm migrating {source} to {target}")) {
                        // Using new KifaFile instances (especially the target one) so that it
                        // reflects after-move state.
                        var f = new KifaFile(source);
                        f.Move(f.GetFilePrefixed("/" + targetCell));
                        var t = new KifaFile(target);
                        if (!t.Exists() || f.Exists()) {
                            Console.WriteLine(
                                $"{file}:(!) error, file {t} should exist ({t.Exists()}, while file {f} should not ({f.Exists()}.");
                        } else {
                            t.Add();
                            f.Unregister();
                            Console.WriteLine($"{file}:(+) moved.");
                        }
                    } else {
                        Console.WriteLine($"{file}:(-) skipped.");
                    }

                    processed.Add(info.Sha256, info.Id);
                    continue;
                }

                if (!sourceFound && targetFound) {
                    if (DryRun) {
                        Console.WriteLine($"{file}:(0) file moved, but linking needs fixing.");
                    } else {
                        var t = new KifaFile(target);
                        t.Add();

                        var f = new KifaFile(source);
                        f.Unregister();
                        Console.WriteLine($"{file}:(*) already moved, link fixed.");
                    }

                    processed.Add(info.Sha256, info.Id);
                    continue;
                }

                Console.WriteLine(
                    $"{file}:(!) error, as source is found {sourceFound} and target is found {targetFound}. It should be different.");
                processed.Add(info.Sha256, info.Id);
                continue;
            }

            Console.WriteLine(
                $"{file}:(!) error, as source is registered {sourceRegistered} and target is registered {targetRegistered}. It should be different.");
            processed.Add(info.Sha256, info.Id);
        }

        return 0;
    }
}
