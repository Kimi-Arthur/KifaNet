using CommandLine;
using Kifa.Api.Files;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

public abstract class DownloadCommand : YoutubeCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('p', "prefix", HelpText = "Prefix of file name. Possible values: date, number")]
    public string? Prefix { get; set; }

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    KifaFile BaseFolder => OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;

    int downloadCounter;

    protected void Download(YouTubeVideo video, string? alternativeFolder = null) {
        var outputFolder = BaseFolder;
        var desiredName = video.GetDesiredName(alternativeFolder: alternativeFolder,
            prefix: GetPrefix(video));
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
            Logger.Info($"Found {message}. Will link instead.");
            KifaFile.LinkAll(found, targetFiles);
            return;
        }

        var canonicalTargetFile = targetFiles[0];

        Logger.Debug($"Downloading video {video.Id} with yt-dlp to {canonicalTargetFile}...");
        var tempFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.mp4");

        YouTubeVideo.DownloadVideo(video.Id.Checked(), tempFile.GetLocalPath());

        tempFile.Move(canonicalTargetFile);
        Logger.Debug($"Downloaded video {video.Id} to {canonicalTargetFile}.");

        KifaFile.LinkAll(canonicalTargetFile, targetFiles);
    }

    string? GetPrefix(YouTubeVideo video)
        => Prefix switch {
            "date" => video.UploadDate != null ? $"{video.UploadDate:yyyy-MM-dd}" : null,
            "number" => $"{++downloadCounter:D2}",
            _ => null
        };
}
