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

        var bestFiles = video.FormatId != null ? [targetFiles[0], desiredFile] : targetFiles;
        var foundBest = KifaFile.FindOne(bestFiles);
        if (foundBest != null) {
            var message = foundBest.ExistsSomewhere()
                ? $"{foundBest.Id} exists in the system"
                : $"{foundBest} exists locally";
            Logger.Info($"Found {message}. Will link instead.");
            KifaFile.LinkAll(foundBest, targetFiles);
            return;
        }

        var canonicalTargetFile = targetFiles[0];

        var tempFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.mp4");

        try {
            Logger.Debug($"Downloading video {video.Id} with yt-dlp to {canonicalTargetFile}...");
            YouTubeVideo.DownloadVideo(video.Id.Checked(), tempFile.GetLocalPath());

            tempFile.Move(canonicalTargetFile);
            Logger.Debug($"Downloaded video {video.Id} to {canonicalTargetFile}.");

            KifaFile.LinkAll(canonicalTargetFile, targetFiles);
        } catch (Exception ex) {
            if (tempFile.Exists()) {
                try {
                    tempFile.Delete();
                } catch {
                }
            }

            var nonsuffixFile = GetCanonicalFile(desiredFile.Host, $"{video.Id}.mp4");
            var foundNonsuffix = KifaFile.FindOne([nonsuffixFile]);
            if (foundNonsuffix != null) {
                var message = foundNonsuffix.ExistsSomewhere()
                    ? $"{foundNonsuffix.Id} exists in the system"
                    : $"{foundNonsuffix} exists locally";
                Logger.Warn(ex,
                    $"Failed to download video {video.Id}. Found nonsuffix version {message}. Will link instead.");

                var nonsuffixDesiredName = video.GetDesiredName(
                    alternativeFolder: alternativeFolder, prefix: GetPrefix(video),
                    includeFormat: false);
                var nonsuffixDesiredFile = nonsuffixDesiredName != null
                    ? outputFolder.GetFile($"{nonsuffixDesiredName}.mp4")
                    : null;
                var nonsuffixTargetFiles = video.GetCanonicalNames(includeFormat: false)
                    .Select(f => GetCanonicalFile(desiredFile.Host, $"{f}.mp4"))
                    .Concat(nonsuffixDesiredFile != null ? [nonsuffixDesiredFile] : [])
                    .ToList();

                KifaFile.LinkAll(foundNonsuffix, nonsuffixTargetFiles);
                return;
            }

            throw;
        }
    }

    string? GetPrefix(YouTubeVideo video)
        => Prefix switch {
            "date" => video.UploadDate != null ? $"{video.UploadDate:yyyy-MM-dd}" : null,
            "number" => $"{++downloadCounter:D2}",
            _ => null
        };
}
