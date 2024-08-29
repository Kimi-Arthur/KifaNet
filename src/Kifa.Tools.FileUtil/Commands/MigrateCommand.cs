using System;
using System.Collections.Generic;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Cloud.Google;
using Kifa.IO;
using Kifa.Jobs;
using Kifa.Service;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("migrate", HelpText = "Migrate Google Drive files to a new cell and quick check on it.")]
public class MigrateCommand : KifaCommand {
    readonly Dictionary<string, string> processed = [];

    [Option('d', "dry-run", HelpText = "Only dry run, not actually migrate.")]
    public bool DryRun { get; set; } = false;

    [Option('q', "quiet", HelpText = "Quiet mode, no confirmation is requested.")]
    public bool QuietMode { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        foreach (var (file, info) in FileInformation.Client.List()) {
            ExecuteItem(file, () => ProcessOneFile(info));
        }

        return LogSummary();
    }

    KifaActionResult ProcessOneFile(FileInformation info) {
        if (info.Sha256 == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"no sha256."
            };
        }

        if (processed.TryGetValue(info.Sha256, out var processedFile)) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"sha256 {info.Sha256} is already processed by {processedFile}."
            };
        }

        processed.Add(info.Sha256, info.Id);

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
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "already moved."
            };
        }

        if (sourceRegistered == true && targetRegistered == null) {
            var sourceFound = new KifaFile(source).Exists();
            var targetFound = new KifaFile(target).Exists();

            if (sourceFound && !targetFound) {
                if (DryRun) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Pending,
                        Message = "(dryrun) to be moved."
                    };
                }

                if (!QuietMode && !Confirm($"Confirm migrating {source} to {target}")) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message = "instructed to skip."
                    };
                }

                // Using new KifaFile instances (especially the target one) so that it
                // reflects after-move state.
                var f = new KifaFile(source);
                f.Move(f.GetFilePrefixed("/" + targetCell));
                var t = new KifaFile(target);
                if (!t.Exists() || f.Exists()) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Error,
                        Message =
                            $"before move, file {t} should exist ({t.Exists()}, while file {f} should not ({f.Exists()}."
                    };
                }

                t.Add();
                f.Unregister();
                return new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = "moved."
                };
            }

            if (!sourceFound && targetFound) {
                if (DryRun) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Pending,
                        Message = "(dryrun) file moved, but linking needs fixing."
                    };
                }

                var t = new KifaFile(target);
                var f = new KifaFile(source);
                t.Add();
                f.Unregister();

                return new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = "already moved, link fixed."
                };
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message =
                    $"source is found {sourceFound} and target is found {targetFound}. It should be different."
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.Error,
            Message =
                $"source is registered {sourceRegistered} and target is registered {targetRegistered}. It should be different."
        };
    }
}
