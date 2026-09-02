using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Media;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("organize", HelpText = "Organize and rename media files with unified naming format.")]
public class OrganizeCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('t', "target-folder", Required = true,
        HelpText = "Target folder to copy media files to.")]
    public required string TargetFolder { get; set; }

    [Option('m', "move", HelpText = "Move files instead of copying.")]
    public bool Move { get; set; } = false;

    [Value(0, Required = true, HelpText = "Target file(s) or folder(s) to process.")]
    public required IEnumerable<string> FileNames { get; set; }

    static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) {
        "jpg", "jpeg", "heic", "heif", "png", "webp", "gif", "tif", "tiff",
        "arw", "cr2", "cr3", "nef", "dng", "raw",
        "mp4", "mov", "m4v", "mkv", "avi"
    };

    static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase) {
        "jpg", "jpeg", "heic", "heif", "png", "webp", "gif", "tif", "tiff",
        "arw", "cr2", "cr3", "nef", "dng", "raw"
    };

    public override int Execute(KifaTask? task = null) {
        var rawFiles = KifaFile.FindExistingFiles(FileNames).ToList();
        var mediaFiles = rawFiles.Where(f => SupportedExtensions.Contains(f.Extension)).ToList();

        if (mediaFiles.Count == 0) {
            Logger.Warn("No supported media files found.");
            return 0;
        }

        var destinationFolder = new KifaFile(TargetFolder);

        // Group companion files (e.g. Live Photo .heic + .mov, RAW + JPG)
        var companionGroups = GroupCompanionFiles(mediaFiles);
        var plannedOperations = new List<(KifaFile Source, KifaFile Target)>();
        var reservedTargetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in companionGroups) {
            var primaryFile = group.OrderByDescending(f => PhotoExtensions.Contains(f.Extension))
                .First();

            DateTime? fileModified = null;
            try {
                var localPath = primaryFile.GetLocalPath();
                if (File.Exists(localPath)) {
                    fileModified = File.GetLastWriteTimeUtc(localPath);
                }
            } catch {
                // Ignore if not accessible as local file
            }

            MediaMetadata metadata;
            using (var stream = primaryFile.OpenRead()) {
                metadata = MediaMetadataService.Extract(stream, primaryFile.Name, fileModified);
            }

            var sourceTag = MediaTagResolver.ResolveSourceTag(metadata,
                (prompt, suggested) => Confirm(prompt, suggested));

            // Collision resolution
            var sequence = (int?) null;
            var baseName = metadata.FormatBaseName(sourceTag, sequence);

            while (group.Any(file => {
                       var candidateTarget = $"{metadata.FormatBaseName(sourceTag, sequence)}.{file.Extension.ToLowerInvariant()}";
                       return reservedTargetNames.Contains(candidateTarget) ||
                              destinationFolder.GetFile(candidateTarget).Exists();
                   })) {
                sequence = (sequence ?? 0) + 1;
            }

            var finalBaseName = metadata.FormatBaseName(sourceTag, sequence);

            foreach (var file in group) {
                var targetFileName = $"{finalBaseName}.{file.Extension.ToLowerInvariant()}";
                reservedTargetNames.Add(targetFileName);
                plannedOperations.Add((file, destinationFolder.GetFile(targetFileName)));
            }
        }

        var selectedOperations = SelectMany(plannedOperations,
            op => $"{op.Source} => {op.Target}",
            "media files to organize");

        if (selectedOperations.Status != KifaActionStatus.OK) {
            ExecuteItem("media files to organize", () => selectedOperations);
            return LogSummary();
        }

        var actionName = Move ? "Move" : "Copy";
        foreach (var (source, target) in selectedOperations.Value) {
            ExecuteItem($"{actionName} {source} => {target}", () => {
                if (target.Exists()) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message = $"Target file {target} already exists."
                    };
                }

                if (Move) {
                    source.Move(target);
                } else {
                    source.Copy(target);
                }

                return KifaActionResult.Success();
            });
        }

        return LogSummary();
    }

    static List<List<KifaFile>> GroupCompanionFiles(List<KifaFile> files) {
        return files.GroupBy(f => $"{f.Parent.GetLocalPath()}/{f.BaseName}")
            .Select(g => g.ToList())
            .ToList();
    }
}
