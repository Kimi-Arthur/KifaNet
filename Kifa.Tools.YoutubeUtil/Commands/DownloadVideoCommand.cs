using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("video", HelpText = "Download YouTube video.")]
public class DownloadVideoCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Video ids from YouTube.")]
    public IEnumerable<string> ids { get; set; }

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    public override int Execute(KifaTask? task = null) {
        var selectedVideos = SelectMany(
            YouTubeVideo.Client.Get(ids.ToList()).ExceptNull()
                .OrderBy(video => video.GetDesiredName()).ToList(),
            video => video.GetDesiredName() ?? video.Id.Checked(), "YouTube videos to download");
        foreach (var video in selectedVideos) {
            ExecuteItem(video.GetDesiredName() ?? video.Id.Checked(),
                () => KifaActionResult.FromAction(() => DownloadVideo(video)));
        }

        return LogSummary();
    }

    void DownloadVideo(YouTubeVideo video) {
        var outputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;
        var desiredName = video.GetDesiredName();
        if (desiredName == null) {
            throw new KifaExecutionException($"No desired name is found for {video.Id}");
        }

        var desiredFile = outputFolder.GetFile($"{desiredName}.mp4");
        var targetFiles = video.GetCanonicalNames()
            .Select(f => GetCanonicalFile(desiredFile.Host, $"{f}.mp4")).Append(desiredFile)
            .ToList();

        var found = KifaFile.FindOne(targetFiles);
        if (found != null) {
            var message = found.ExistsSomewhere()
                ? $"{found.Id} exists in the system"
                : $"{found} exists locally";
            Logger.Info($"Found {message}. Link instead.");
            KifaFile.LinkAll(found, targetFiles);
            return;
        }

        Logger.Error("Downloading is not supported yet.");
    }

    static KifaFile GetCanonicalFile(string host, string name)
        => new($"{host}{Configs.BasePath}/{name}");
}
