using System;
using System.Collections.Generic;
using CommandLine;
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

            var source = $"google:good/$/{info.Sha256}.v1";
            var target = $"google:{GoogleDriveStorageClient.DefaultCell}/$/{info.Sha256}.v1";

            bool? foundSource = null, foundTarget = null;

            foreach (var (location, upload) in info.Locations) {
                if (location == source) {
                    foundSource = upload != null;
                }

                if (location == target) {
                    foundTarget = upload != null;
                }
            }

            if (foundTarget == true && foundSource == null) {
                Console.WriteLine($"{file}:(1) already moved.");
                processed.Add(info.Sha256, info.Id);
                continue;
            }

            if (foundSource == true && foundTarget == null) {
                if (DryRun) {
                    Console.WriteLine($"{file}:(0) to move.");
                } else if (QuietMode || Confirm($"Confirm migrating {source} to {target}")) {
                    Console.WriteLine($"{file}:(+) moved.");
                } else {
                    Console.WriteLine($"{file}:(-) skipped.");
                }

                processed.Add(info.Sha256, info.Id);
                continue;
            }

            Console.WriteLine(
                $"{file}:(!) error as source is {foundSource} and target is {foundTarget}");
            processed.Add(info.Sha256, info.Id);
        }

        return 0;
    }
}
