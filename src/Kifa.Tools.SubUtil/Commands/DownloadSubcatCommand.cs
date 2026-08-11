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
        var searchBaseName = Confirm($"Confirm search text for {videoFile}:",
            videoFile.GetSubtitleFile().BaseName);

        if (searchBaseName == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = $"Search cancelled for {videoFile}."
            };
        }

        var expandedChoices = SubcatClient.FindSubtitles(searchBaseName);

        if (expandedChoices.Count == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"No subtitles found for {videoFile}."
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
            var sourcesPath = SubcatClient.GetSourcesPath(videoFile.ParentPath);
            var (origContent, origFilename) = SubcatClient.DownloadOriginal(choice);

            // Save original subtitle
            var origTarget = new KifaFile($"{KifaFile.SubtitlesHost}{sourcesPath}/{origFilename}");
            if (origTarget.Exists()) {
                if (Force && Confirm($"Subtitle file {origTarget} already exists. Replace it?")) {
                    origTarget.Write(origContent);
                    downloadedCount++;
                    totalBytes += origContent.Length;
                } else {
                    Logger.Info($"Skipped already downloaded subtitle {origTarget}.");
                }
            } else {
                origTarget.Write(origContent);
                downloadedCount++;
                totalBytes += origContent.Length;
            }

            // Translate locally and save target language subtitles
            foreach (var lang in TargetLanguages) {
                var langSubcatCode = SubcatClient.GetSubcatLanguage(lang);
                var langFilename = SubcatClient.GetSubtitleFileName(choice.OriginalLink, lang);
                var langTarget = new KifaFile($"{KifaFile.SubtitlesHost}{sourcesPath}/{langFilename}");

                if (langTarget.Exists()) {
                    if (!Force || !Confirm($"Subtitle file {langTarget} already exists. Replace it?")) {
                        Logger.Info($"Skipped already downloaded subtitle {langTarget}.");
                        continue;
                    }
                }

                var (translatedContent, _) = SubcatClient.TranslateSrt(origContent, langSubcatCode);
                langTarget.Write(translatedContent);
                downloadedCount++;
                totalBytes += translatedContent.Length;
            }
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
