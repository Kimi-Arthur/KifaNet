using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("move",
    HelpText =
        "Move subtitle files from local folders to SubtitlesHost and remove local files.")]
public class MoveCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true,
        HelpText = "Target file(s) or directory(ies) containing subtitle files to move.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    [Option('f', "force", HelpText = "Prompt for overwrite if a conflict is present.")]
    public bool Force { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var existingFiles = KifaFile.FindExistingFiles(FileNames, recursive: true);
        var subtitleFiles = existingFiles.Where(file
            => file.Extension != null &&
               Common.SubtitleExtensions.Contains(file.Extension.ToLower())).ToList();

        if (subtitleFiles.Count == 0) {
            Logger.Warn("No subtitle files found to move.");
            return 0;
        }

        var selected = SelectMany(subtitleFiles, file => file.ToString(), "subtitle files to move");
        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("subtitle files to move", () => selected);
            return LogSummary();
        }

        foreach (var subtitleFile in selected.Value) {
            ExecuteItem(subtitleFile.ToString(), () => MoveSubtitle(subtitleFile));
        }

        return LogSummary();
    }

    KifaActionResult MoveSubtitle(KifaFile sourceFile) {
        var targetFile = new KifaFile($"{KifaFile.SubtitlesHost}{sourceFile.Path}");
        var sourceSha = sourceFile.CalculateInfo(FileProperties.Sha256).Sha256;

        if (targetFile.Exists()) {
            var targetSha = targetFile.CalculateInfo(FileProperties.Sha256).Sha256;
            var isSameContent = sourceSha != null && sourceSha == targetSha;

            if (isSameContent) {
                Logger.Info(
                    $"Target file {targetFile} already exists with matching SHA256 ({targetSha}).");
            } else {
                // Conflict detected (different hashes)
                if (!Force) {
                    Logger.Warn(
                        $"Conflict detected for {targetFile} (source SHA256 {sourceSha} vs target SHA256 {targetSha}), but -f (force) was not specified. Skipping overwrite.");
                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message =
                            $"Conflict detected for {targetFile}. Skipped overwrite because -f was not specified."
                    };
                }

                if (!Confirm(
                        $"Conflict detected: target file {targetFile} exists with different SHA256 (source {sourceSha} vs target {targetSha}). Overwrite?")) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.Skipped,
                        Message = $"Skipped overwriting conflicting target file {targetFile}."
                    };
                }

                targetFile.Delete();
                sourceFile.Copy(targetFile, neverLink: true);
                Logger.Info($"Overwrote {targetFile} with content from {sourceFile}.");
            }
        } else {
            sourceFile.Copy(targetFile, neverLink: true);
            Logger.Info($"Copied subtitle content from {sourceFile} to {targetFile}.");
        }

        var finalTargetSha = targetFile.CalculateInfo(FileProperties.Sha256).Sha256;
        if (sourceSha == null || sourceSha != finalTargetSha) {
            Logger.Error(
                $"Content match check failed for {targetFile} (source SHA256 {sourceSha} vs target SHA256 {finalTargetSha}). Keeping local source file {sourceFile}.");
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Content match check failed for {targetFile}. Source file kept."
            };
        }

        if (Confirm($"Confirm removing local source file {sourceFile}?")) {
            RemoveSourceFile(sourceFile);
            Logger.Info($"Removed local source file {sourceFile}.");
        } else {
            Logger.Info($"Kept local source file {sourceFile}.");
        }

        return KifaActionResult.Success();
    }

    void RemoveSourceFile(KifaFile sourceFile) {
        if (sourceFile.Registered) {
            KifaFile.RemoveLogical(sourceFile.Id, force: true);
        } else {
            sourceFile.RemoveInstance(force: true);
        }
    }
}
