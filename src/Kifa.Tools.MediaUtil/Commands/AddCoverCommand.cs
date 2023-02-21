using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("cover", HelpText = "Add cover to media file.")]
public class AddCoverCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('o', "output-folder", Default = "outputs", HelpText = "Folder to put output files.")]
    public string OutputFolder { get; set; }

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var files = KifaFile.FindExistingFiles(FileNames, pattern: "*.mp4", recursive: false);
        var filesWithImages = files.Select(source
            => (source, cover: GetImageFile(source), target: GetTargetFile(source))).ToList();
        var skippedFiles = filesWithImages.Where(file => file.cover == null || file.target.Exists())
            .ToList();
        if (skippedFiles.Count > 0) {
            Logger.Warn($"Skipping the following {skippedFiles.Count} files:");
            foreach (var file in skippedFiles) {
                var reason = file.target.Exists() ? "target exists" : "image not found";
                Logger.Warn($"\t{file.source}: {reason}");
            }

            if (!Confirm($"Safely ignore the {skippedFiles.Count} files above?")) {
                return 1;
            }
        }

        var selectedFiles = SelectMany(
            filesWithImages.Where(file => file.cover != null && !file.target.Exists())
                .Select(file => (file.source, cover: file.cover!, file.target)).ToList(),
            choice => $"{choice.source} + {choice.cover} => {choice.target}", "files");

        var filesByResults = selectedFiles.Select(file => (file, result: AddCover(file)))
            .GroupBy(item => item.result.Status == KifaActionStatus.OK)
            .ToDictionary(item => item.Key, item => item.ToList());

        if (filesByResults.ContainsKey(true)) {
            var successes = filesByResults[true];
            Logger.Info($"Successfully added cover for the following {successes.Count} files:");
            foreach (var success in successes) {
                Logger.Info(
                    $"\t{success.file.source} + {success.file.cover} => {success.file.target}");
            }
        }

        if (filesByResults.ContainsKey(false)) {
            var failures = filesByResults[false];
            Logger.Error($"Failed to add cover for the following {failures.Count} files:");
            foreach (var failure in failures) {
                Logger.Error($"\t{failure.file.source}: {failure.result.Message}");
            }

            return 1;
        }

        return 0;
    }

    KifaActionResult AddCover((KifaFile source, KifaFile cover, KifaFile target) file) {
        Directory.GetParent(file.target.GetLocalPath()).Create();

        return KifaActionResult.FromExecutionResult(Executor.Run("ffmpeg",
            $"-i \"{file.source.GetLocalPath()}\" -i \"{file.cover.GetLocalPath()}\" " +
            "-map 0 -c copy -map 1 -disposition:v:1 attached_pic " +
            $"\"{file.target.GetLocalPath()}\""));
    }

    static readonly List<string> ExpectedCoverExtensions = new() {
        "jpg",
        "png"
    };

    KifaFile? GetImageFile(KifaFile sourceFile)
        => ExpectedCoverExtensions
            .Select(ext => sourceFile.Parent.GetFile($"{sourceFile.BaseName}.{ext}"))
            .FirstOrDefault(file => file.Exists());

    KifaFile GetTargetFile(KifaFile sourceFile)
        => sourceFile.Parent.GetFile($"{OutputFolder}/{sourceFile.Name}");
}
