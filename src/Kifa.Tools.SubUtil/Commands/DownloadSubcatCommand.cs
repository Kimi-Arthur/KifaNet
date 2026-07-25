using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Html;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.Subtitle.Subcat;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("subcat", HelpText = "Download subtitle files from https://www.subtitlecat.com/.")]
public class DownloadSubcatCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly HttpClient HttpClient = new();

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
            ExecuteItem(file.ToString(), () => DownloadSubtitle(file));
        }

        return LogSummary();
    }

    const string UrlPrefix = SubcatClient.UrlPrefix;

    KifaActionResult DownloadSubtitle(KifaFile videoFile) {
        var searchBaseName = videoFile.GetSubtitleFile().BaseName;
        var doc = HttpClient.SendWithRetry($"{UrlPrefix}/index.php?search={searchBaseName}")
            .GetString().GetDocument();
        var table = doc.GetElementsByClassName("sub-table").FirstOrDefault();
        var elements = table?.QuerySelectorAll("a");

        if (elements == null || !elements.Any()) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"No subtitles found for {videoFile}."
            };
        }

        var rawSubtitles = elements.Take(10).Select(element => (
            Title: element.Parent.Checked().TextContent,
            Link: element.Attributes["href"].Checked().Value)).ToList();

        var expandedChoices = rawSubtitles.SelectMany(sub => {
            var pageLink = $"{UrlPrefix}/{sub.Link}";
            var pageContent = HttpClient.SendWithRetry(pageLink).GetString().GetDocument();
            var downloadLinks = SubcatClient.GetDownloadLinks(pageContent, TargetLanguages);
            return downloadLinks.Select(kv => (Language: kv.Key, Title: sub.Title, Link: sub.Link,
                DownloadLink: kv.Value));
        }).ToList();

        if (expandedChoices.Count == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message =
                    $"No subtitles found with requested languages ({TargetLanguages.Select(l => l.Code).JoinBy(", ")}) for {videoFile}."
            };
        }

        var selected = SelectMany(expandedChoices,
            choice
                => $"[{choice.Language.Code}{(choice.DownloadLink.NeedsGeneration ? "*" : "")}] {choice.Title}: {choice.Link}",
            $"subtitles for {searchBaseName}");

        if (selected.Status != KifaActionStatus.OK) {
            return new KifaActionResult {
                Status = selected.Status,
                Message = selected.Message
            };
        }

        var downloadedCount = 0;
        var totalBytes = 0L;

        foreach (var choice in selected.Value) {
            var path =
                SubcatClient.GetSubtitlePath(videoFile.ParentPath, choice.Link, choice.Language);
            var target = new KifaFile($"{KifaFile.SubtitlesHost}{path}");
            if (target.Exists()) {
                if (!Force || !Confirm($"Subtitle file {target} already exists. Replace it?")) {
                    Logger.Info($"Skipped already downloaded subtitle {target}.");
                    continue;
                }
            }

            var content = SubcatClient.DownloadOrGenerate(choice.DownloadLink, choice.Language);
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
