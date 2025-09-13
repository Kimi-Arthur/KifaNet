using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using YoutubeDLSharp;

namespace Kifa.YouTube;

public class YouTubeVideo : DataModel, WithModelId<YouTubeVideo> {
    public static string ModelId => "youtube/videos";

    public static KifaServiceClient<YouTubeVideo> Client { get; set; } =
        new KifaServiceRestClient<YouTubeVideo>();

    public static string? YoutubeDlPath { get; set; } = "/opt/homebrew/bin/yt-dlp";

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

    public override DateTimeOffset? Fill() {
        var ytdl = new YoutubeDL {
            YoutubeDLPath = YoutubeDlPath
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

        return null;
    }

    public List<string> GetCanonicalNames(string? formatId = null)
        => [$"{Id}.{formatId ?? FormatId}", Id.Checked()];

    public string? GetDesiredName(string? formatId = null)
        => "{author}/{title}.{id},{format_id}".FormatIfNonNull(null,
            ("author", Author?.NormalizeFileName()), ("title", Title?.NormalizeFileName()),
            ("id", Id), ("format_id", formatId ?? FormatId));
}
