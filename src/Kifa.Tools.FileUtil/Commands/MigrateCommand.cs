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

            bool? sourceRegistered = null, targetRegistered = null;

            foreach (var (location, upload) in info.Locations) {
                if (location == source) {
                    sourceRegistered = upload != null;
                }

                if (location == target) {
                    targetRegistered = upload != null;
                }
            }

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
                        var f = new KifaFile(source);
                        f.Move(f.GetFilePrefixed("/" + targetCell));
                        var t = new KifaFile(target);
                        t.Add();
                        f.Unregister();
                        Console.WriteLine($"{file}:(+) moved.");
                    } else {
                        Console.WriteLine($"{file}:(-) skipped.");
                    }
                }

                if (!sourceFound && targetFound) {
                    if (DryRun) {
                        Console.WriteLine($"{file}:(0) file moved, but linking needs fixing.");
                    } else {
                        var t = new KifaFile(target);
                        t.Add();

                        var f = new KifaFile(source);
                        f.Unregister();
                        Console.WriteLine($"{file}:(*) moved.");
                    }
                }

                processed.Add(info.Sha256, info.Id);
                continue;
            }

            Console.WriteLine(
                $"{file}:(!) error as source is {sourceRegistered} and target is {targetRegistered}");
            processed.Add(info.Sha256, info.Id);
        }

        return 0;
    }
}
