using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Service;
using NLog;
using YoutubeDLSharp;

namespace Kifa.YouTube;

public class YouTubeVideo : DataModel, WithModelId<YouTubeVideo> {
    public static string ModelId => "youtube/videos";

    public static KifaServiceClient<YouTubeVideo> Client { get; set; } =
        new KifaServiceRestClient<YouTubeVideo>();

    public override bool FillByDefault => true;

    public static string? YoutubeDownloaderPath { get; set; }

    public string? Title { get; set; }
    public string? Author { get; set; }
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

        return FillWithFindYoutubeVideo();
    }

    DateTimeOffset? FillWithYoutubeDl() {
        var ytdl = new YoutubeDL {
            YoutubeDLPath = YoutubeDownloaderPath
        };
        var metadata = ytdl.RunVideoDataFetch(Id).GetAwaiter().GetResult();
        if (!metadata.Success) {
            throw new UnableToFillException(
                $"Cannot find video info for {Id}: {metadata.ErrorOutput.JoinBy("\n")}");
        }

        var videoData = metadata.Data;
        Title = videoData.Title;
        Author = videoData.Uploader;
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

    public List<string> GetCanonicalNames(string? formatId = null)
        => [$"{Id}.{formatId ?? FormatId}", Id.Checked()];

    public string? GetDesiredName(string? formatId = null)
        => "{author}/{title}.{id},{format_id}".FormatIfNonNull(null,
            ("author", Author?.NormalizeFileName()), ("title", Title?.NormalizeFileName()),
            ("id", Id), ("format_id", formatId ?? FormatId));
}
