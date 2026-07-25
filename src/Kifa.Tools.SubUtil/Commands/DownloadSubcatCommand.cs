using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.Subtitle.Subcat;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("subcat", HelpText = "Download subtitle files from https://www.subtitlecat.com/.")]
public class DownloadSubcatCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target files to download subtitles for.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('f', "force", HelpText = "Try to get the subtitle even if it exists.")]
    public bool Force { get; set; }

    [Option('l', "languages", Separator = ',',
        HelpText = "Languages to download. Default is 'zh'.")]
    public IEnumerable<string> Languages { get; set; } = ["zh"];

    public List<Language> TargetLanguages
        => Languages.Select(l => (Language) l).Distinct().ToList();

    public override int Execute(KifaTask? task = null) {
        var files = FileNames.Select(f => new KifaFile(f)).ToList();
        var selected = SelectMany(files, file => file.ToString(), "files to download subtitle for");
        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to download subtitle for", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value) {
            ExecuteItem(file.ToString(), () => DownloadSubtitles(file));
        }

        return LogSummary();
    }

    KifaActionResult DownloadSubtitles(KifaFile videoFile) {
        var searchBaseName = videoFile.GetSubtitleFile().BaseName;
        var expandedChoices = SubcatClient.FindSubtitles(searchBaseName, TargetLanguages);

        if (expandedChoices.Count == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message =
                    $"No subtitles found with requested languages ({TargetLanguages.Select(l => l.Code).JoinBy(", ")}) for {videoFile}."
            };
        }

        var selected = SelectMany(expandedChoices, choice => choice.ToString(),
            $"subtitles for {searchBaseName}", reverse: true);

        if (selected.Status != KifaActionStatus.OK) {
            return new KifaActionResult {
                Status = selected.Status,
                Message = selected.Message
            };
        }

        var downloadedCount = 0;
        var totalBytes = 0L;

        foreach (var choice in selected.Value) {
            // We may be able to skip downloading but it complicates the logic for generation.
            var (content, filename) = SubcatClient.DownloadOrGenerate(choice);
            var sourcesPath = SubcatClient.GetSourcesPath(videoFile.ParentPath);
            var target = new KifaFile($"{KifaFile.SubtitlesHost}{sourcesPath}/{filename}");

            if (target.Exists()) {
                if (!Force || !Confirm($"Subtitle file {target} already exists. Replace it?")) {
                    Logger.Info($"Skipped already downloaded subtitle {target}.");
                    continue;
                }
            }

            target.Write(content);
            downloadedCount++;
            totalBytes += content.Length;
        }

        if (downloadedCount == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"Skipped downloading subtitles for {videoFile}."
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message =
                $"Successfully written {totalBytes} bytes to {downloadedCount} subtitle file(s)."
        };
    }
}
