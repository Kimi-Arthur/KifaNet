using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using Kifa.ArchiveOrg;
using Kifa.Html;
using Kifa.Service;
using NLog;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace Kifa.YouTube;

public class YouTubeChapter {
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string? Title { get; set; }
}

public class YouTubeVideo : DataModel, WithModelId<YouTubeVideo> {
    public static string ModelId => "youtube/videos";

    public static KifaServiceClient<YouTubeVideo> Client { get; set; } =
        new KifaServiceRestClient<YouTubeVideo>();

    public override bool FillByDefault => true;

    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(365);

    public static string YoutubeDownloaderPath {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public static string CookiesPath {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public static string? PluginPath { get; set; }

    public static List<string>? ExtractorArgs { get; set; }

    public static YoutubeDL YoutubeDL {
        get {
            var ytdl = new YoutubeDL();
            try {
                ytdl.YoutubeDLPath = YoutubeDownloaderPath;
            } catch (NullReferenceException) {
            }

            return ytdl;
        }
    }

    public static OptionSet GetOptionSet(bool flatPlaylist = false) {
        var options = new OptionSet {
            FlatPlaylist = flatPlaylist,
            EmbedMetadata = !flatPlaylist,
            EmbedThumbnail = !flatPlaylist
        };

        try {
            options.Cookies = CookiesPath;
        } catch (NullReferenceException) {
        }

        if (PluginPath != null) {
            options.PluginDirs = PluginPath;
        }

        if (ExtractorArgs != null) {
            options.ExtractorArgs = new MultiValue<string>(ExtractorArgs.ToArray());
        }

        return options;
    }

    public static OptionSet OptionSet => GetOptionSet();

    public static void DownloadVideo(string videoId, string? filePath = null,
        string? outputFolder = null, string? outputFileTemplate = null) {
        var ytdl = YoutubeDL;
        if (filePath != null) {
            ytdl.OutputFolder = Path.GetDirectoryName(filePath);
            ytdl.OutputFileTemplate = $"{Path.GetFileNameWithoutExtension(filePath)}.%(ext)s";
        }

        if (outputFolder != null) {
            ytdl.OutputFolder = outputFolder;
        }

        if (outputFileTemplate != null) {
            ytdl.OutputFileTemplate = outputFileTemplate;
        }

        var downloadResult = ytdl.RunVideoDownload(
                videoId, mergeFormat: DownloadMergeFormat.Mp4, overrideOptions: OptionSet)
            .GetAwaiter()
            .GetResult();

        if (!downloadResult.Success) {
            throw new Exception(
                $"Failed to download video {videoId}: {string.Join("\n", downloadResult.ErrorOutput)}");
        }
    }

    public static (List<string> TrackPaths, string? CoverPath) DownloadTracks(string videoId,
        string targetPath, YouTubeVideo? video = null) {
        var parentFolder = Path.GetDirectoryName(targetPath) ?? ".";
        var basePrefix = Path.GetFileNameWithoutExtension(targetPath);

        var ytdl = YoutubeDL;
        ytdl.OutputFolder = parentFolder;

        var options = GetOptionSet();
        options.EmbedMetadata = false;
        options.EmbedThumbnail = false;
        options.WriteThumbnail = true;
        options.ConvertThumbnails = "png";
        options.Output = $"{basePrefix}.%(format_id)s.%(ext)s";
        options.AddCustomOption("-o", $"thumbnail:{basePrefix}.c.%(ext)s");

        List<string> formatIds = [];
        if (video?.FormatId != null) {
            formatIds = video.FormatId.Split("+").ToList();
            options.Format = string.Join(",", formatIds);
        } else {
            options.Format = "bestvideo,bestaudio/best";
        }

        var result = ytdl.RunVideoDownload(videoId, overrideOptions: options).GetAwaiter()
            .GetResult();
        if (!result.Success) {
            throw new Exception(
                $"Failed to download video tracks for {videoId}: {string.Join("\n", result.ErrorOutput)}");
        }

        var downloadedFiles = Directory.GetFiles(parentFolder);
        var trackPaths = new List<string>();

        if (formatIds.Count > 0) {
            foreach (var fId in formatIds) {
                var match = downloadedFiles.FirstOrDefault(f
                    => Path.GetFileNameWithoutExtension(f) == $"{basePrefix}.{fId}");
                if (match != null) {
                    trackPaths.Add(match);
                }
            }
        }

        if (trackPaths.Count == 0) {
            trackPaths = downloadedFiles.Where(f => {
                var name = Path.GetFileNameWithoutExtension(f);
                return name.StartsWith($"{basePrefix}.") && !name.EndsWith(".c");
            }).OrderBy(f => f).ToList();
        }

        var coverPath = downloadedFiles.FirstOrDefault(f
            => Path.GetFileNameWithoutExtension(f) == $"{basePrefix}.c");
        if (coverPath != null && Path.GetExtension(coverPath)
                .Equals(".webp", StringComparison.OrdinalIgnoreCase)) {
            var pngCover = Path.Combine(parentFolder, $"{basePrefix}.c.png");
            var convertResult = Executor.Run("ffmpeg",
                $"-i \"{coverPath}\" -update 1 -bitexact -y \"{pngCover}\"");
            if (convertResult.ExitCode == 0) {
                try {
                    File.Delete(coverPath);
                } catch {
                }

                coverPath = pngCover;
            }
        }

        return (trackPaths, coverPath);
    }

    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? AuthorId { get; set; }
    public Date? UploadDate { get; set; }
    public string? Description { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<YouTubeChapter> Chapters { get; set; } = new();

    public TimeSpan Duration { get; set; }
    public double Fps { get; set; }
    public long Width { get; set; }
    public long Height { get; set; }
    public string? Codec { get; set; }
    public string? FormatId { get; set; }
    public string? Thumbnail { get; set; }

    static readonly HttpClient HttpClient = new();
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Fill() {
        try {
            FillWithYoutubeDl();
            return;
        } catch (Exception e) {
            Logger.Warn(e);
        }

        try {
            FillWithFindYoutubeVideo();
            return;
        } catch (Exception e) {
            Logger.Warn(e);
        }

        if (!FillWithWayback()) {
            throw new UnableToFillException($"Failed to find info for {Id}");
        }
    }

    void FillWithYoutubeDl() {
        var metadata = YoutubeDL.RunVideoDataFetch(Id, overrideOptions: OptionSet).GetAwaiter()
            .GetResult();
        if (!metadata.Success) {
            throw new UnableToFillException(
                $"Cannot find video info for {Id}: {metadata.ErrorOutput.JoinBy("\n")}");
        }

        var videoData = metadata.Data;
        Title = videoData.Title;
        Author = videoData.Uploader;
        AuthorId = videoData.UploaderID ?? videoData.ChannelID;
        UploadDate = videoData.UploadDate;
        Description = videoData.Description;
        Categories = videoData.Categories.ToList();
        Tags = videoData.Tags.ToList();
        Duration = TimeSpan.FromSeconds(videoData.Duration.Checked());

        FormatId = videoData.FormatID;
        var formatIds = FormatId.Split("+");
        var videoFormat = videoData.Formats.First(f => f.FormatId == formatIds[0]);
        Fps = videoFormat.FrameRate.Checked();
        Width = videoFormat.Width.Checked();
        Height = videoFormat.Height.Checked();
        Codec = NormalizeCodec(videoFormat.VideoCodec);
        Thumbnail = videoData.Thumbnail;
        Chapters = videoData.Chapters?.Select(c => new YouTubeChapter {
            StartTime = c.StartTime ?? 0,
            EndTime = c.EndTime ?? 0,
            Title = c.Title
        }).ToList() ?? new();
    }

    void FillWithFindYoutubeVideo() {
        var fybResponse = HttpClient.Call(new FindYoutubeVideoRpc(Id.Checked()));
        var archiveItem =
            fybResponse?.Keys.FirstOrDefault(key
                => key.Archived && key.Name == "Archive.org Details");
        if (archiveItem == null) {
            throw new UnableToFillException(
                $"Cannot find video history with FindYoutubeVideo service: {fybResponse.ToJson()}");
        }

        var archiveLink = archiveItem.Available.FirstOrDefault(link => link.Url != null)?.Url;

        if (archiveLink == null) {
            throw new UnableToFillException(
                $"Cannot find link with FindYoutubeVideo service: {fybResponse.ToJson()}");
        }

        var archiveId = archiveLink.Split("/").Last();
        var archiveMetadata = HttpClient.Call(new ArchiveMetadataRpc(archiveId));
        if (archiveMetadata == null) {
            throw new UnableToFillException($"Cannot find archive for {archiveId}");
        }

        var archiveFile =
            archiveMetadata.Files.FirstOrDefault(f => f.Name.EndsWith($"-{Id}.info.json"));

        if (archiveFile == null) {
            throw new UnableToFillException($"Cannot find item {Id} in archive {archiveId}");
        }

        var archiveFileContent = HttpClient.Call(new ArchiveItemDetailRpc(archiveMetadata.D1,
            archiveMetadata.Dir, archiveFile.Name));

        if (archiveFileContent == null) {
            throw new UnableToFillException(
                $"Cannot find file {archiveFile.Name} in archive {archiveId}");
        }

        Title = archiveFileContent.Title;
        Author = archiveFileContent.Uploader;
        AuthorId = archiveFileContent.UploaderId;
        UploadDate = Date.Parse(archiveFileContent.UploadDate, "yyyyMMdd");
        Description = archiveFileContent.Description;
        Categories = archiveFileContent.Categories.ToList();
        Tags = archiveFileContent.Tags.ToList();
        Duration = TimeSpan.FromSeconds(archiveFileContent.Duration.Checked());

        FormatId = archiveFileContent.FormatId;
        Fps = archiveFileContent.Fps;
        Width = archiveFileContent.Width;
        Height = archiveFileContent.Height;
        Codec = NormalizeCodec(archiveFileContent.Vcodec);
        Thumbnail = archiveFileContent.Thumbnail;
    }


    bool FillWithWayback() {
        var watchUrl = $"https://www.youtube.com/watch?v={Id}";
        var cdxResults = HttpClient.Call(new CdxSearchRpc(watchUrl));
        foreach (var entry in cdxResults.OrderByDescending(r => r.Length)) {
            if (FillWithPageContent(
                    HttpClient.Call(new ArchiveContentRpc(entry.Original, entry.Timestamp)))) {
                return true;
            }
        }

        return false;
    }

    bool FillWithPageContent(string? body) {
        if (body == null) {
            return false;
        }

        var document = body.GetDocument();
        Title = document.QuerySelector("#watch-headline-title > span")?.InnerHtml.Trim();
        if (Title == null) {
            return false;
        }

        Author = document.QuerySelectorAll("#watch7-user-header a")[1].InnerHtml.Trim();
        var date = document.QuerySelector("#eow-date")?.InnerHtml.Trim();
        if (date != null) {
            UploadDate = Date.Parse(date, "MMM d, yyyy");
        }

        Description = document.QuerySelector("#eow-description")?.InnerHtml.Trim();
        Categories = document.QuerySelectorAll("#eow-category > a").Select(c => c.InnerHtml.Trim())
            .ToList();

        Height = int.Parse(document.QuerySelector("meta[itemprop=height]").Checked()
            .Attributes["content"].Checked().Value);
        Width = int.Parse(document.QuerySelector("meta[itemprop=width]").Checked()
            .Attributes["content"].Checked().Value);
        Duration =
            TimeSpan.ParseExact(
                document.QuerySelector("meta[itemprop=duration]").Checked().Attributes["content"]
                    .Checked().Value, @"\P\Tm\Ms\S", CultureInfo.InvariantCulture);
        return true;
    }

    static string? NormalizeCodec(string? rawCodec) {
        if (rawCodec == null) {
            return null;
        }

        var codec = rawCodec.ToLowerInvariant();
        if (codec.StartsWith("avc") || codec.StartsWith("h264")) {
            return "avc";
        }

        if (codec.StartsWith("vp09") || codec.StartsWith("vp9")) {
            return "vp9";
        }

        if (codec.StartsWith("av01") || codec.StartsWith("av1")) {
            return "av1";
        }

        if (codec.StartsWith("hev1") || codec.StartsWith("hvc1") || codec.StartsWith("hevc") ||
            codec.StartsWith("h265")) {
            return "hevc";
        }

        return codec.Split('.')[0];
    }

    public string? GetSuffix(string? formatId = null) {
        var segments = new List<string>();
        if (Width > 0 && Height > 0) {
            var resolution = Fps > 0 ? $"{Width}x{Height}p{Fps}" : $"{Width}x{Height}p";
            var codec = NormalizeCodec(Codec);
            if (codec != null && codec != "avc") {
                resolution += $"-{codec}";
            }

            segments.Add(resolution);
        }

        var fid = formatId ?? FormatId;
        if (fid != null) {
            segments.Add(fid);
        }

        return segments.Count > 0 ? segments.JoinBy(".") : null;
    }

    public List<string> GetCanonicalNames(string? formatId = null) {
        var suffix = GetSuffix(formatId);
        return [string.FormatOr($"{Id}.{suffix}", Id.Checked())];
    }

    // Common file extension suffix (e.g., ".mp4") length.
    const int TypeSuffixLength = 4;

    public string? GetDesiredName(string? formatId = null, string? alternativeFolder = null,
        string? extraFolder = null, string? prefix = null, string? explicitSuffix = null) {
        if (Title == null || Id == null) {
            return null;
        }

        var title = Title.NormalizeFileName();
        var uploaderName = Author?.NormalizeFileName();
        var uploaderId = AuthorId?.NormalizeFileName();

        var defaultFolder = string.FormatOr($"{uploaderName?.Choppable()}.{uploaderId}",
            uploaderName?.Choppable() ?? uploaderId);

        var folder = alternativeFolder != null
            ? $"{alternativeFolder.Split('/')[0].Choppable()}.youtube/{alternativeFolder.Split('/').Skip(1).JoinBy('/')}"
                .TrimEnd('/')
            : string.FormatOr($"{defaultFolder}.youtube");

        folder = string.FormatOr($"{folder}/{extraFolder}", folder);

        var filenameSegments = new List<string>();
        if (prefix != null) {
            filenameSegments.Add(prefix.NormalizeFileName());
        }

        filenameSegments.Add(title);

        var suffix = explicitSuffix != null
            ? explicitSuffix.Length > 0 ? explicitSuffix : null
            : GetSuffix(formatId);
        var fileName =
            $"{filenameSegments.JoinBy(" ").Choppable()}.{Id}{string.FormatOrEmpty($".{suffix}")}";

        return string.FormatOr($"{folder}/{fileName}", fileName)
            .NormalizeFilePath(TypeSuffixLength);
    }
}
