using System.Text;
using CommandLine;
using Kifa.Api.Files;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

public abstract class DownloadCommand : YoutubeCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('r', "refresh", HelpText = "Force refresh server data before downloading.")]
    public bool Refresh { get; set; } = false;

    [Option('p', "prefix", HelpText = "Prefix of file name. Possible values: date, number")]
    public string? Prefix { get; set; }

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    [Option('e', "prefer-existing",
        HelpText = "Use any existing version of the video. Throw an error if multiple versions exist.")]
    public bool PreferExisting { get; set; } = false;

    KifaFile BaseFolder => OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;

    int downloadCounter;

    protected void Download(YouTubeVideo video, string? alternativeFolder = null,
        string? extraFolder = null) {
        var outputFolder = BaseFolder;
        var desiredName = video.GetDesiredName(alternativeFolder: alternativeFolder,
            extraFolder: extraFolder, prefix: GetPrefix(video));
        if (desiredName == null) {
            throw new KifaExecutionException($"No desired name is found for {video.Id}");
        }

        var desiredFile = outputFolder.GetFile($"{desiredName}.mp4");
        var canonicalFiles = video.GetCanonicalNames()
            .Select(f => GetCanonicalFile(desiredFile.Host, $"{f}.mp4")).ToList();
        var targetFiles = canonicalFiles.Append(desiredFile).ToList();

        if (PreferExisting) {
            var existingRepoFiles = FindExistingRepoFiles(video, desiredFile.Host);
            if (existingRepoFiles.Count > 1) {
                var foundList = string.Join(", ", existingRepoFiles.Select(f => f.ToString()));
                throw new KifaExecutionException(
                    $"Multiple existing versions found in RepoPath for {video.Id}: {foundList}");
            }

            if (existingRepoFiles.Count == 1) {
                LinkExistingRepoFile(existingRepoFiles[0], video, alternativeFolder, extraFolder,
                    outputFolder);
                return;
            }
        } else {
            var found = KifaFile.FindOne(canonicalFiles);
            if (found != null) {
                Logger.Info($"Found existing repo file {found}. Linking to {desiredFile}.");
                KifaFile.LinkAll(found, targetFiles);
                return;
            }
        }

        var canonicalTargetFile = targetFiles[0];

        var tempTargetFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.mp4");

        try {
            Logger.Debug($"Downloading video tracks for {video.Id} with yt-dlp...");
            var (trackPaths, coverPath) = YouTubeVideo.DownloadTracks(video.Id.Checked(),
                tempTargetFile.GetLocalPath(), video);

            var trackFiles = trackPaths.Select(p => new KifaFile(p)).ToList();
            var coverFile = coverPath != null ? new KifaFile(coverPath) : null;

            Logger.Debug($"Merging tracks for {video.Id} to {canonicalTargetFile}...");
            canonicalTargetFile.Delete();
            MergePartFiles(trackFiles, coverFile, canonicalTargetFile, video);
            Logger.Debug($"Merged tracks for {video.Id} to {canonicalTargetFile}.");

            foreach (var p in trackFiles) {
                p.Delete();
            }

            coverFile?.Delete();
            Logger.Debug("Removed temp files.");

            KifaFile.LinkAll(canonicalTargetFile, targetFiles);
        } catch {
            foreach (var file in canonicalTargetFile.Parent.List().Where(f
                         => f.BaseName.StartsWith(
                             $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}"))) {
                try {
                    file.Delete();
                } catch {
                }
            }

            throw;
        }
    }

    static void MergePartFiles(List<KifaFile> parts, KifaFile? cover, KifaFile target,
        YouTubeVideo video) {
        if (parts.Count == 2 && !HasVideoStream(parts[0]) && HasVideoStream(parts[1])) {
            (parts[0], parts[1]) = (parts[1], parts[0]);
        }

        var metaFile =
            target.Parent.GetFile($"{KifaFile.DefaultIgnoredPrefix}{target.BaseName}.meta");
        var sb = new StringBuilder();
        sb.AppendLine(";FFMETADATA1");
        if (video.Title != null) {
            sb.AppendLine($"title={EscapeFfmetadata(video.Title)}");
        }

        if (video.Author != null) {
            sb.AppendLine($"artist={EscapeFfmetadata(video.Author)}");
        }

        if (video.UploadDate != null) {
            sb.AppendLine($"date={video.UploadDate:yyyyMMdd}");
        }

        if (video.Id != null) {
            sb.AppendLine($"comment=https://www.youtube.com/watch?v={video.Id}");
        }

        if (video.Description != null) {
            sb.AppendLine($"description={EscapeFfmetadata(video.Description)}");
        }

        if (video.Categories.Count > 0) {
            sb.AppendLine($"genre={EscapeFfmetadata(video.Categories[0])}");
        }

        if (video.Chapters.Count > 0) {
            foreach (var chapter in video.Chapters) {
                sb.AppendLine();
                sb.AppendLine("[CHAPTER]");
                sb.AppendLine("TIMEBASE=1/1000");
                sb.AppendLine($"START={(long) Math.Round(chapter.StartTime * 1000)}");
                sb.AppendLine($"END={(long) Math.Round(chapter.EndTime * 1000)}");
                if (chapter.Title != null) {
                    sb.AppendLine($"title={EscapeFfmetadata(chapter.Title)}");
                }
            }
        }

        metaFile.Write(sb.ToString());

        try {
            var inputs = parts.Select(f => $"-i \"{f.GetLocalPath()}\"").ToList();
            var coverIndex = -1;
            if (cover != null && cover.Exists()) {
                coverIndex = inputs.Count;
                inputs.Add($"-i \"{cover.GetLocalPath()}\"");
            }

            var metaIndex = inputs.Count;
            inputs.Add($"-i \"{metaFile.GetLocalPath()}\"");

            var maps = new List<string>();
            if (parts.Count == 2) {
                maps.Add("-map 0:v -map 1:a");
            } else {
                maps.Add("-map 0");
            }

            if (coverIndex >= 0) {
                var coverVideoIndex = parts.Any(HasVideoStream) ? 1 : 0;
                maps.Add($"-map {coverIndex} -disposition:v:{coverVideoIndex} attached_pic");
            }

            var chapterArg = video.Chapters.Count > 0 ? $"-map_chapters {metaIndex}" : "";

            var commandArgs = $"{string.Join(" ", inputs)} " +
                              $"{string.Join(" ", maps)} -c copy " +
                              $"-map_metadata {metaIndex} {chapterArg} -bitexact -y " +
                              $"\"{target.GetLocalPath()}\"";

            var result = Executor.Run("ffmpeg", commandArgs);
            if (result.ExitCode != 0) {
                throw new Exception($"Merging files failed: {result.StandardError}");
            }
        } finally {
            metaFile.Delete();
        }
    }

    static bool HasVideoStream(KifaFile file) {
        var res = Executor.Run("ffprobe",
            $"-v error -select_streams v:0 -show_entries stream=codec_type -of default=noprint_wrappers=1:nokey=1 \"{file.GetLocalPath()}\"");
        return res.ExitCode == 0 && res.StandardOutput.Trim() == "video";
    }

    static string EscapeFfmetadata(string value)
        => value.Replace(@"\", @"\\").Replace("=", @"\=").Replace(";", @"\;").Replace("#", @"\#")
            .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\\n");

    string? GetPrefix(YouTubeVideo video)
        => Prefix switch {
            "date" => video.UploadDate != null ? $"{video.UploadDate:yyyy-MM-dd}" : null,
            "number" => $"{++downloadCounter:D2}",
            _ => null
        };

    void LinkExistingRepoFile(KifaFile repoFile, YouTubeVideo video,
        string? alternativeFolder, string? extraFolder, KifaFile outputFolder) {
        var explicitSuffix = repoFile.BaseName == video.Id
            ? ""
            : repoFile.BaseName[$"{video.Id}.".Length..];
        var matchedDesiredName = video.GetDesiredName(alternativeFolder: alternativeFolder,
            extraFolder: extraFolder, prefix: GetPrefix(video), explicitSuffix: explicitSuffix);
        var matchedDesiredFile = matchedDesiredName != null
            ? outputFolder.GetFile($"{matchedDesiredName}.mp4")
            : null;
        var matchedTargetFiles = matchedDesiredFile != null
            ? new List<KifaFile> { repoFile, matchedDesiredFile }
            : new List<KifaFile> { repoFile };

        if (matchedDesiredFile != null) {
            Logger.Info($"Found existing repo file {repoFile}. Linking to {matchedDesiredFile}.");
        } else {
            Logger.Info($"Found existing repo file {repoFile}.");
        }
        KifaFile.LinkAll(repoFile, matchedTargetFiles);
    }

    static List<KifaFile> FindExistingRepoFiles(YouTubeVideo video, string host)
        => KifaFile.FindAllFiles([host + RepoPath], recursive: false, pattern: $"{video.Id}.*")
            .Where(f => (f.BaseName == video.Id || f.BaseName.StartsWith($"{video.Id}.")) &&
                        f.Extension == "mp4")
            .DistinctBy(f => f.Id)
            .ToList();
}
