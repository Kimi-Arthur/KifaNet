using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using FFMpegCore;
using Kifa.Api.Files;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("extract", HelpText = "Extract subtitle from video files.")]
class ExtractCommand : KifaCommand {
    const string SubtitlesPrefix = "/Subtitles";
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly Dictionary<string, string> SubtitleExtensions = new() {
        { "subrip", "srt" },
        { "ass", "ass" },
        { "hdmv_pgs_subtitle", "sup" }
    };

    [Option('g', "group", HelpText = "Group name for the source, like SMURF, 人人影视.")]
    public string? Group { get; set; }

    [Option('f', "force", HelpText = "Forcing extracting the subtitle.")]
    public bool Force { get; set; }

    [Value(0, Required = true, HelpText = "Files to combine.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        var (_, foundFiles) = KifaFile.FindExistingFiles(FileNames);
        foundFiles = foundFiles.Where(f => !Common.SubtitleExtensions.Contains(f.Extension))
            .ToList();

        foreach (var file in foundFiles) {
            Console.WriteLine(file);
        }

        if (!Confirm(
                $"Confirm extracting subtitles of the {foundFiles.Count} above and place in relevant folders in /Subtitles?")) {
            Logger.Info("Gave up extrating. Exiting.");
            return 0;
        }

        var allResults = new List<(string item, KifaActionResult result)>();
        foreach (var file in foundFiles) {
            var info = FFProbe.Analyse(file.GetLocalPath());
            var (choice, index) = SelectOne(info.SubtitleStreams,
                s => s.Language +
                     (s.Tags?.ContainsKey("title") ?? false ? $" ({s.Tags["title"]})" : "") +
                     $" => {GetExtractedSubtitleFile(file, s)}");

            allResults.Add((file.ToString(),
                Logger.LogResult(ExtractSubtitle(file, choice), file.ToString())));
        }

        return LogSummary(allResults);
    }

    static int LogSummary(List<(string item, KifaActionResult result)> allResults) {
        var resultsByStatus = allResults.GroupBy(item => item.result.Status == KifaActionStatus.OK)
            .ToDictionary(item => item.Key, item => item.ToList());
        if (resultsByStatus.ContainsKey(true)) {
            var items = resultsByStatus[true];
            Logger.Info($"Successfully acted on the following {items.Count} items:");
            foreach (var (item, result) in items) {
                Logger.Info($"{item}:");
                foreach (var line in (result.Message ?? "OK").Split("\n")) {
                    Logger.Info($"\t{line}");
                }
            }
        }

        if (resultsByStatus.ContainsKey(false)) {
            var items = resultsByStatus[false];
            Logger.Error($"Failed to act on the following {items.Count} items:");
            foreach (var (item, result) in items) {
                Logger.Error($"{item} =>");
                foreach (var line in (result.Message ?? "OK").Split("\n")) {
                    Logger.Error($"\t{line}");
                }
            }

            return 1;
        }

        return 0;
    }

    KifaActionResult ExtractSubtitle(KifaFile file, SubtitleStream choice) {
        var subtitleFile = GetExtractedSubtitleFile(file, choice);
        if (subtitleFile.Exists()) {
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Subtitle file {subtitleFile} already exists. Skipped"
            };
        }

        subtitleFile.EnsureLocalParent();
        var result = Executor.Run("ffmpeg",
            $"-i \"{file.GetLocalPath()}\" -map 0:{choice.Index} -c copy \"{subtitleFile.GetLocalPath()}\"");
        if (result.ExitCode != 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = result.ToString()
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully created {subtitleFile}"
        };
    }

    KifaFile GetExtractedSubtitleFile(KifaFile file, SubtitleStream subtitle)
        => file.Parent
            .GetFile(file.BaseName + "." + subtitle.Language[..subtitle.Language.IndexOf("-")] +
                     (Group != null ? $"-{Group}." : ".") + SubtitleExtensions[subtitle.CodecName])
            .GetFilePrefixed(SubtitlesPrefix);
}
