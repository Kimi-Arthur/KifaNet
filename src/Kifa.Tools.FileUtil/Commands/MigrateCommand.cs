using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Cloud.Google;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("migrate", HelpText = "Migrate Google Drive files to a new cell and quick check on it.")]
public class MigrateCommand : KifaCommand {
    public override int Execute() {
        var files = FileInformation.Client.List();
        var processed = new HashSet<string>();
        foreach (var (file, info) in files) {
            if (info.Sha256 == null) {
                Console.WriteLine($"{file}: no sha256");
                continue;
            }

            if (processed.Contains(info.Sha256)) {
                Console.WriteLine($"{file}: sha256 already processed.");
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
                Console.WriteLine($"{file}: already moved.");
                processed.Add(info.Sha256);
                continue;
            }

            if (foundSource == true && foundTarget == null) {
                Console.WriteLine($"{file}: to move.");
                processed.Add(info.Sha256);
                continue;
            }

            Console.WriteLine(
                $"{file}: error as source is {foundSource} and target is {foundTarget}");
            processed.Add(info.Sha256);
        }

        return 0;
    }
}
