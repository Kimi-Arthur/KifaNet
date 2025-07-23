using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Html;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("subcat", HelpText = "Download subtitle files from https://www.subtitlecat.com/.")]
class DownloadSubcatCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly HttpClient HttpClient = new();

    const string UrlPrefix = "https://www.subtitlecat.com";

    [Value(0, Required = true, HelpText = "Target files to download subtitles for.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('f', "force", HelpText = "Try to get the subtitle even if it exists.")]
    public bool Force { get; set; }

    public override int Execute(KifaTask? task = null) {
        var files = FileNames.Select(f => new KifaFile(f)).ToList();
        var selected = SelectMany(files, choiceToString: file => file.ToString(),
            choicesName: "files");

        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => DownloadSubtitle(file));
        }

        return LogSummary();
    }

    KifaActionResult DownloadSubtitle(KifaFile videoFile) {
        var target = videoFile.GetSubtitleFile("zh.srt");
        if (target.Exists()) {
            if (!Force || !Confirm($"Subtitle file {target} already exists. Replace it?")) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message = $"Skipped already downloaded subtitle {target}."
                };
            }
        }

        var doc = HttpClient.GetAsync($"{UrlPrefix}/index.php?search={target.BaseName}")
            .GetAwaiter().GetResult().GetString().GetDocument();
        var elements = doc.GetElementsByClassName("sub-table").Single().QuerySelectorAll("a");
        var subtitles = elements.Take(10).Select(element => (
            Title: element.Parent.Checked().TextContent,
            Link: element.Attributes["href"].Checked().Value)).ToList();

        var choice = SelectOne(subtitles, sub => $"{sub.Title}: {sub.Link}", "subtitle",
            startingIndex: 1, reverse: true);

        if (choice == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "No choice given. Download is canceled."
            };
        }

        var pageLink = $"{UrlPrefix}/{choice.Value.Choice.Link}";
        Logger.Debug($"Will obtain download link from {pageLink}");

        var pageContent = HttpClient.GetAsync(pageLink).GetAwaiter().GetResult().GetString()
            .GetDocument();
        ;

        var link =
            $"{UrlPrefix}/{pageContent.GetElementById("download_zh-CN").Checked().Attributes["href"]
                .Checked().Value}";
        Logger.Debug($"Will download subtitle from {link}");

        var content = HttpClient.GetAsync(link).GetAwaiter().GetResult().GetString();
        target.Write(content);

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully written {content.Length} bytes to {target}."
        };
    }
}
