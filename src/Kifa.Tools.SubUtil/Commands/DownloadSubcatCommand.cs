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

    [Option('o', "original-only",
        HelpText = "Download only the original subtitle without machine translation.")]
    public bool OriginalOnly { get; set; }

    [Option("sources-only",
        HelpText =
            "Save raw subtitle files under /Sources/.../subcat only without importing to the live folder.")]
    public bool SourcesOnly { get; set; }

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
            return KifaActionResult.Cancelled($"Search cancelled for {videoFile}.");
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
            var (originalContent, originalFilename) = SubcatClient.DownloadOriginal(choice);

            // Save untouched raw original subtitle under /Sources/.../subcat
            var originalTarget =
                new KifaFile($"{KifaFile.SubtitlesHost}{sourcesPath}/{originalFilename}");
            TryWriteSubtitleFile(originalTarget, originalContent, ref downloadedCount,
                ref totalBytes);

            var originalLanguage = SubcatClient.ResolveSourceLanguage(choice);

            if (OriginalOnly) {
                if (!SourcesOnly) {
                    var liveLanguageCode =
                        originalLanguage != null && originalLanguage != Language.Unknown
                            ? originalLanguage.Code
                            : Confirm($"Confirm language code for {choice.Title}:", "en") ??
                              "original";
                    var liveTarget = videoFile.GetSubtitleFile($"subcat.{liveLanguageCode}.srt");
                    TryWriteSubtitleFile(liveTarget, SubcatClient.CleanSrt(originalContent),
                        ref downloadedCount, ref totalBytes);
                }

                continue;
            }

            // Translate locally (or reuse original when language already matches) and save both raw + live subtitles
            foreach (var lang in TargetLanguages) {
                var langFilename = SubcatClient.GetSubtitleFileName(choice.OriginalLink, lang);
                var langSourceTarget =
                    new KifaFile($"{KifaFile.SubtitlesHost}{sourcesPath}/{langFilename}");
                var liveTarget = videoFile.GetSubtitleFile($"subcat.{lang.Code}.srt");

                var needSource = !langSourceTarget.Exists() || Force;
                var needLive = !SourcesOnly && (!liveTarget.Exists() || Force);
                if (!needSource && !needLive) {
                    Logger.Info(
                        $"Skipped already existing subtitle {langSourceTarget} and {liveTarget}.");
                    continue;
                }

                string rawLangContent;
                if (originalLanguage == lang) {
                    Logger.Debug(
                        $"Reusing original subtitle directly for '{lang.Code}' without machine translation.");
                    rawLangContent = originalContent;
                } else {
                    var langSubcatCode = SubcatClient.GetSubcatLanguage(lang);
                    (rawLangContent, _) = SubcatClient.TranslateSrt(originalContent, langSubcatCode);
                }

                TryWriteSubtitleFile(langSourceTarget, rawLangContent, ref downloadedCount,
                    ref totalBytes);

                if (!SourcesOnly) {
                    TryWriteSubtitleFile(liveTarget, SubcatClient.CleanSrt(rawLangContent),
                        ref downloadedCount, ref totalBytes);
                }
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

    bool TryWriteSubtitleFile(KifaFile target, string content, ref int downloadedCount,
        ref long totalBytes) {
        if (target.Exists()) {
            if (!Force || !Confirm($"Subtitle file {target} already exists. Replace it?")) {
                Logger.Info($"Skipped already downloaded subtitle {target}.");
                return false;
            }
        }

        target.Write(content);
        downloadedCount++;
        totalBytes += content.Length;
        return true;
    }
}
