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

public class YouTubeVideo : DataModel, WithModelId<YouTubeVideo> {
    public static string ModelId => "youtube/videos";

    public static KifaServiceClient<YouTubeVideo> Client { get; set; } =
        new KifaServiceRestClient<YouTubeVideo>();

    public override bool FillByDefault => true;

    public static string YoutubeDownloaderPath {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public static string CookiesPath {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public static YoutubeDL YoutubeDL => new() {
        YoutubeDLPath = YoutubeDownloaderPath
    };

    public static OptionSet OptionSet => new() {
        Cookies = CookiesPath
    };

    public static void DownloadVideo(string videoId, string? filePath = null, string? outputFolder = null, string? outputFileTemplate = null) {
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
            videoId, overrideOptions: OptionSet).GetAwaiter().GetResult();

        if (!downloadResult.Success) {
            throw new Exception(
                $"Failed to download video {videoId}: {string.Join("\n", downloadResult.ErrorOutput)}");
        }
    }

    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? AuthorId { get; set; }
    public Date? UploadDate { get; set; }
    public string? Description { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public TimeSpan Duration { get; set; }
    public double Fps { get; set; }
    public long Width { get; set; }
    public long Height { get; set; }
    public string? FormatId { get; set; }
    public string? Thumbnail { get; set; }

    static readonly HttpClient HttpClient = new();
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override DateTimeOffset? Fill() {
        try {
            return FillWithYoutubeDl();
        } catch (Exception e) {
            Logger.Warn(e);
        }

        try {
            return FillWithFindYoutubeVideo();
        } catch (Exception e) {
            Logger.Warn(e);
        }

        return FillWithWayback();
    }

    DateTimeOffset? FillWithYoutubeDl() {
        var metadata = YoutubeDL.RunVideoDataFetch(Id, overrideOptions: OptionSet).GetAwaiter().GetResult();
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
        Thumbnail = videoData.Thumbnail;

        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }

    DateTimeOffset? FillWithFindYoutubeVideo() {
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
        Thumbnail = archiveFileContent.Thumbnail;

        return DateTimeOffset.Now + TimeSpan.FromDays(365 * 10);
    }


    DateTimeOffset? FillWithWayback() {
        var watchUrl = $"https://www.youtube.com/watch?v={Id}";
        var cdxResults = HttpClient.Call(new CdxSearchRpc(watchUrl));
        foreach (var entry in cdxResults.OrderByDescending(r => r.Length)) {
            if (FillWithPageContent(
                    HttpClient.Call(new ArchiveContentRpc(entry.Original, entry.Timestamp)))) {
                return DateTimeOffset.Now + TimeSpan.FromDays(365 * 10);
            }
        }

        return null;
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

    public List<string> GetCanonicalNames(string? formatId = null)
        => [$"{Id}.{formatId ?? FormatId}", Id.Checked()];

    public string? GetDesiredName(string? formatId = null, string? alternativeFolder = null,
        string? prefix = null) {
        var defaultFolder = string.FormatOr($"{Author?.NormalizeFileName()}.{AuthorId?.NormalizeFileName()}") ?? Author?.NormalizeFileName();
        var folder = (alternativeFolder ?? defaultFolder)?.NormalizeFileName();
        if (folder != null) {
            var folderSegments = folder.Split('/').ToList();
            folderSegments[0] += ".youtube";
            folder = folderSegments.JoinBy("/");
        }

        var title = Title?.NormalizeFileName();
        var fid = formatId ?? FormatId;

        if (folder != null) {
            return prefix != null
                ? string.FormatOr($"{folder}/{prefix} {title}.{Id}.{fid}")
                : string.FormatOr($"{folder}/{title}.{Id}.{fid}");
        }

        return prefix != null
            ? string.FormatOr($"{prefix} {title}.{Id}.{fid}")
            : string.FormatOr($"{title}.{Id}.{fid}");
    }
}
