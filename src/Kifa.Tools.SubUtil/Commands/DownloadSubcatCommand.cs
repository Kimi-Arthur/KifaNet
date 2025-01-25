using System.Linq;
using System.Net.Http;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Html;
using Kifa.Jobs;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("subcat", HelpText = "Download subtitle file from https://www.subtitlecat.com/.")]
class DownloadSubcatCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly HttpClient HttpClient = new();

    static readonly string UrlPrefix = "https://www.subtitlecat.com";

    [Value(0, Required = true, HelpText = "Target file to download subtitle for.")]
    public string FileUri { get; set; }

    public override int Execute(KifaTask? task = null) {
        var target = new KifaFile(FileUri).GetSubtitleFile("zh.srt");

        var doc = HttpClient.GetAsync($"{UrlPrefix}/index.php?search={target.BaseName}")
            .GetAwaiter().GetResult().GetString().GetDocument();
        var elements = doc.GetElementsByClassName("sub-table").Single().QuerySelectorAll("a");
        var subtitles = elements.Take(10).Select(element => (
            Title: element.Parent.Checked().TextContent,
            Link: element.Attributes["href"].Checked().Value)).ToList();

        var choice = SelectOne(subtitles, sub => $"{sub.Title}: {sub.Link}", "subtitle",
            startingIndex: 1, reverse: true);

        if (choice == null) {
            Logger.Fatal("No choice given. Download is canceled.");
            return 1;
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
        Logger.Trace(content);

        target.Write(content);

        Logger.Info($"Successfully written {content.Length} bytes to {target}.");
        return 0;
    }
}
