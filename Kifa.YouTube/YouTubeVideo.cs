using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Kifa.ArchiveOrg;
using Kifa.Html;
using Kifa.Service;
using Newtonsoft.Json.Linq;
using NLog;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
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

    public static List<string>? ExtractorArgs { get; set; } = ["youtubetab:skip=authcheck"];

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

    public static List<string> ExtractVideoIds(IEnumerable<VideoData>? entries) {
        if (entries == null) {
            return [];
        }

        var result = new List<string>();
        foreach (var entry in entries) {
            if (entry.Entries is { Length: > 0 }) {
                result.AddRange(ExtractVideoIds(entry.Entries));
            } else if (!string.IsNullOrEmpty(entry.ID)) {
                result.Add(entry.ID);
            }
        }

        return result.Distinct().ToList();
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

    public static OptionSet GetTrackDownloadOptionSet(string parentFolder, string basePrefix,
        YouTubeVideo? video = null) {
        var options = GetOptionSet();
        options.Paths = parentFolder;
        options.EmbedMetadata = false;
        options.EmbedThumbnail = false;
        options.WriteThumbnail = true;
        options.ConvertThumbnails = "png";
        options.Output = $"{basePrefix}.%(format_id)s.%(ext)s";
        options.AddCustomOption("-o", $"thumbnail:{basePrefix}.c.%(ext)s");

        if (video?.FormatId != null) {
            var formatIds = video.FormatId.Split("+").ToList();
            options.Format = string.Join(",", formatIds);
        } else {
            options.Format = "bestvideo,bestaudio/best";
        }

        return options;
    }

    public static (List<string> TrackPaths, string? CoverPath) DownloadTracks(string videoId,
        string targetPath, YouTubeVideo? video = null) {
        var parentFolder = Path.GetFullPath(Path.GetDirectoryName(targetPath) ?? ".");
        var basePrefix = Path.GetFileNameWithoutExtension(targetPath);

        Directory.CreateDirectory(parentFolder);

        foreach (var file in Directory.GetFiles(parentFolder, $"{basePrefix}.*")) {
            try {
                File.Delete(file);
            } catch {
            }
        }

        var ytdl = YoutubeDL;
        ytdl.OutputFolder = parentFolder;

        var options = GetTrackDownloadOptionSet(parentFolder, basePrefix, video);

        var result = ytdl.RunVideoDownload(videoId, overrideOptions: options).GetAwaiter()
            .GetResult();
        if (!result.Success) {
            throw new Exception(
                $"Failed to download video tracks for {videoId}: {string.Join("\n", result.ErrorOutput)}");
        }

        var downloadedFiles = Directory.GetFiles(parentFolder);
        var trackPaths = new List<string>();

        List<string> formatIds = [];
        if (video?.FormatId != null) {
            formatIds = video.FormatId.Split("+").ToList();
        }

        if (formatIds.Count > 0) {
            foreach (var fId in formatIds) {
                var match = downloadedFiles.FirstOrDefault(f
                    => Path.GetFileNameWithoutExtension(f) == $"{basePrefix}.{fId}");
                if (match != null) {
                    trackPaths.Add(match);
                }
            }
        }

        if (trackPaths.Count == 0 || (formatIds.Count > 0 && trackPaths.Count < formatIds.Count)) {
            trackPaths = downloadedFiles.Where(f => {
                var name = Path.GetFileNameWithoutExtension(f);
                return name.StartsWith($"{basePrefix}.") && !name.EndsWith(".c");
            }).OrderBy(f => f).ToList();
        }

        if (trackPaths.Count == 0) {
            throw new Exception(
                $"No downloaded tracks found for {videoId} with prefix {basePrefix} in {parentFolder}.");
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

    static readonly Regex YouTubeIdRegex = new(@"^[a-zA-Z0-9_-]{11}$", RegexOptions.Compiled);
    static readonly Regex DurationHoursRegex =
        new(@"(\d+)\s*h", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex DurationMinRegex =
        new(@"(\d+)\s*min", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex DurationSecRegex =
        new(@"(\d+)\s*s(?!\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex DurationMsRegex =
        new(@"(\d+)\s*ms", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static YouTubeVideo? FromMediaFile(string filePath) {
        var result = Executor.Run("mediainfo", $"\"{filePath}\"");
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput)) {
            var video = FromMediaInfo(result.StandardOutput, filePath);
            if (video != null) {
                return video;
            }
        }

        var ffprobeResult = Executor.Run("ffprobe",
            $"-v error -show_format -of json \"{filePath}\"");
        if (ffprobeResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(ffprobeResult.StandardOutput)) {
            return FromFfprobeJson(ffprobeResult.StandardOutput, filePath);
        }

        return null;
    }

    public static YouTubeVideo? FromMediaInfo(string mediaInfoText, string? filePath = null) {
        if (string.IsNullOrWhiteSpace(mediaInfoText)) {
            return null;
        }

        if (mediaInfoText.TrimStart().StartsWith('{')) {
            return FromMediaInfoJson(mediaInfoText, filePath) ??
                   FromFfprobeJson(mediaInfoText, filePath);
        }

        return FromMediaInfoText(mediaInfoText, filePath);
    }

    static YouTubeVideo? FromMediaInfoText(string text, string? filePath) {
        var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var generalDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inGeneral = false;

        foreach (var line in lines) {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) {
                continue;
            }

            if (!trimmed.Contains(':')) {
                if (trimmed.Equals("General", StringComparison.OrdinalIgnoreCase)) {
                    inGeneral = true;
                    continue;
                }

                if (inGeneral) {
                    break;
                }
            }

            if (inGeneral || (!text.Contains("General\n", StringComparison.OrdinalIgnoreCase) &&
                              !text.Contains("General\r\n", StringComparison.OrdinalIgnoreCase))) {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0) {
                    var key = line[..colonIndex].Trim();
                    var value = line[(colonIndex + 1)..].Trim();
                    generalDict.TryAdd(key, value);
                }
            }
        }

        var completeName = generalDict.GetValueOrDefault("Complete name") ?? filePath;
        var videoId = ExtractVideoId(completeName);
        if (videoId == null) {
            return null;
        }

        var video = new YouTubeVideo {
            Id = videoId
        };

        if (generalDict.TryGetValue("Title", out var title) && title.Length > 0) {
            video.Title = title;
        }

        var author = generalDict.GetValueOrDefault("Performer") ??
                     generalDict.GetValueOrDefault("Artist") ??
                     generalDict.GetValueOrDefault("Author") ??
                     generalDict.GetValueOrDefault("Uploader");
        if (author != null && author.Length > 0) {
            video.Author = author;
            var uploader = YouTubeUploader.Get(author);
            if (uploader != null) {
                video.AuthorId = uploader.Id;
                video.Author = uploader.Name ?? video.Author;
            }
        }

        var description = generalDict.GetValueOrDefault("Description") ??
                          generalDict.GetValueOrDefault("Comment");
        if (description != null && description.Length > 0) {
            video.Description = description;
        }

        var dateStr = generalDict.GetValueOrDefault("Recorded date") ??
                      generalDict.GetValueOrDefault("Recorded_Date") ??
                      generalDict.GetValueOrDefault("Encoded date") ??
                      generalDict.GetValueOrDefault("Encoded_Date") ??
                      generalDict.GetValueOrDefault("Tagged date") ??
                      generalDict.GetValueOrDefault("Tagged_Date") ??
                      generalDict.GetValueOrDefault("Date");
        var uploadDate = ParseDate(dateStr);
        if (uploadDate != null) {
            video.UploadDate = uploadDate;
        }

        if (generalDict.TryGetValue("Duration", out var durationStr)) {
            video.Duration = ParseDuration(durationStr);
        }

        return video;
    }

    static YouTubeVideo? FromMediaInfoJson(string jsonText, string? filePath) {
        try {
            var json = JObject.Parse(jsonText);
            var media = json["media"];
            if (media == null) {
                return null;
            }

            var tracks = media["track"] as JArray;
            var general = tracks?.FirstOrDefault(t => t["@type"]?.ToString() == "General") as JObject;
            if (general == null) {
                return null;
            }

            var completeName = general["CompleteName"]?.ToString() ??
                               general["FileName"]?.ToString() ?? filePath;
            var videoId = ExtractVideoId(completeName);
            if (videoId == null) {
                return null;
            }

            var video = new YouTubeVideo {
                Id = videoId
            };

            var title = general["Title"]?.ToString();
            if (title != null && title.Length > 0) {
                video.Title = title;
            }

            var author = general["Performer"]?.ToString() ??
                         general["Artist"]?.ToString() ??
                         general["Author"]?.ToString() ??
                         general["Uploader"]?.ToString();
            if (author != null && author.Length > 0) {
                video.Author = author;
                var uploader = YouTubeUploader.Get(author);
                if (uploader != null) {
                    video.AuthorId = uploader.Id;
                    video.Author = uploader.Name ?? video.Author;
                }
            }

            var description = general["Description"]?.ToString() ?? general["Comment"]?.ToString();
            if (description != null && description.Length > 0) {
                video.Description = description;
            }

            var dateStr = general["Recorded_Date"]?.ToString() ??
                          general["Encoded_Date"]?.ToString() ??
                          general["Tagged_Date"]?.ToString();
            var uploadDate = ParseDate(dateStr);
            if (uploadDate != null) {
                video.UploadDate = uploadDate;
            }

            var durationStr = general["Duration"]?.ToString();
            if (durationStr != null && durationStr.Length > 0) {
                video.Duration = ParseDuration(durationStr);
            }

            return video;
        } catch {
            return null;
        }
    }

    static YouTubeVideo? FromFfprobeJson(string jsonText, string? filePath) {
        try {
            var json = JObject.Parse(jsonText);
            var format = json["format"] as JObject;
            if (format == null) {
                return null;
            }

            var completeName = format["filename"]?.ToString() ?? filePath;
            var videoId = ExtractVideoId(completeName);
            if (videoId == null) {
                return null;
            }

            var video = new YouTubeVideo {
                Id = videoId
            };

            var tags = format["tags"] as JObject;
            if (tags != null) {
                var title = tags["title"]?.ToString() ?? tags["TITLE"]?.ToString();
                if (title != null && title.Length > 0) {
                    video.Title = title;
                }

                var author = tags["artist"]?.ToString() ??
                             tags["ARTIST"]?.ToString() ??
                             tags["performer"]?.ToString() ??
                             tags["PERFORMER"]?.ToString() ??
                             tags["uploader"]?.ToString() ??
                             tags["author"]?.ToString();
                if (author != null && author.Length > 0) {
                    video.Author = author;
                    var uploader = YouTubeUploader.Get(author);
                    if (uploader != null) {
                        video.AuthorId = uploader.Id;
                        video.Author = uploader.Name ?? video.Author;
                    }
                }

                var description = tags["description"]?.ToString() ??
                                  tags["DESCRIPTION"]?.ToString() ??
                                  tags["comment"]?.ToString() ??
                                  tags["COMMENT"]?.ToString();
                if (description != null && description.Length > 0) {
                    video.Description = description;
                }

                var dateStr = tags["date"]?.ToString() ??
                              tags["DATE"]?.ToString() ??
                              tags["creation_time"]?.ToString() ??
                              tags["CREATION_TIME"]?.ToString();
                var uploadDate = ParseDate(dateStr);
                if (uploadDate != null) {
                    video.UploadDate = uploadDate;
                }
            }

            var durationStr = format["duration"]?.ToString();
            if (durationStr != null && durationStr.Length > 0) {
                video.Duration = ParseDuration(durationStr);
            }

            return video;
        } catch {
            return null;
        }
    }

    public static string? ExtractVideoId(string? nameOrPath) {
        if (nameOrPath == null || nameOrPath.Length == 0) {
            return null;
        }

        var fileName = Path.GetFileName(nameOrPath);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        if (YouTubeIdRegex.IsMatch(baseName)) {
            return baseName;
        }

        var parts = baseName.Split('.');
        foreach (var part in parts.Reverse()) {
            if (YouTubeIdRegex.IsMatch(part)) {
                return part;
            }
        }

        var match = Regex.Match(baseName, @"[a-zA-Z0-9_-]{11}");
        if (match.Success) {
            return match.Value;
        }

        return baseName;
    }

    public static TimeSpan ParseDuration(string durationStr) {
        if (double.TryParse(durationStr, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var seconds)) {
            return TimeSpan.FromSeconds(seconds);
        }

        var hoursMatch = DurationHoursRegex.Match(durationStr);
        var minMatch = DurationMinRegex.Match(durationStr);
        var secMatch = DurationSecRegex.Match(durationStr);
        var msMatch = DurationMsRegex.Match(durationStr);

        var hours = hoursMatch.Success ? int.Parse(hoursMatch.Groups[1].Value) : 0;
        var minutes = minMatch.Success ? int.Parse(minMatch.Groups[1].Value) : 0;
        var secs = secMatch.Success ? int.Parse(secMatch.Groups[1].Value) : 0;
        var ms = msMatch.Success ? int.Parse(msMatch.Groups[1].Value) : 0;

        if (hours > 0 || minutes > 0 || secs > 0 || ms > 0) {
            return new TimeSpan(0, hours, minutes, secs, ms);
        }

        if (TimeSpan.TryParse(durationStr, CultureInfo.InvariantCulture, out var ts)) {
            return ts;
        }

        return TimeSpan.Zero;
    }

    public static Date? ParseDate(string? dateStr) {
        if (dateStr == null || dateStr.Length == 0) {
            return null;
        }

        dateStr = dateStr.Trim();
        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d1)) {
            return (Date) new DateTime(d1.Year, d1.Month, d1.Day);
        }

        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss 'UTC'",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d2)) {
            return (Date) new DateTime(d2.Year, d2.Month, d2.Day);
        }

        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d3)) {
            return (Date) new DateTime(d3.Year, d3.Month, d3.Day);
        }

        if (DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var dto)) {
            return (Date) new DateTime(dto.Year, dto.Month, dto.Day);
        }

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var dt)) {
            return (Date) new DateTime(dt.Year, dt.Month, dt.Day);
        }

        return null;
    }

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

        try {
            if (FillWithWayback()) {
                return;
            }
        } catch (Exception e) {
            Logger.Warn(e);
        }

        throw new DataNotFoundException($"Failed to find info for {Id}");
    }

    void FillWithYoutubeDl() {
        var metadata = YoutubeDL.RunVideoDataFetch(Id, overrideOptions: OptionSet).GetAwaiter()
            .GetResult();
        if (!metadata.Success) {
            throw new DataNotFoundException(
                $"Cannot find video info for {Id}: {metadata.ErrorOutput.JoinBy("\n")}");
        }

        var videoData = metadata.Data;
        Title = videoData.Title;
        Author = videoData.Uploader;
        AuthorId = videoData.UploaderID ?? videoData.ChannelID;
        UploadDate = videoData.UploadDate;
        Description = videoData.Description;
        Categories = videoData.Categories?.ToList() ?? [];
        Tags = videoData.Tags?.ToList() ?? [];
        Duration = TimeSpan.FromSeconds(videoData.Duration ?? 0);

        FormatId = videoData.FormatID;
        if (FormatId != null && videoData.Formats != null) {
            var formatIds = FormatId.Split("+");
            var videoFormat = videoData.Formats.FirstOrDefault(f => f.FormatId == formatIds[0]);
            if (videoFormat != null) {
                Fps = videoFormat.FrameRate ?? 0;
                Width = videoFormat.Width ?? 0;
                Height = videoFormat.Height ?? 0;
                Codec = NormalizeCodec(videoFormat.VideoCodec);
            }
        }

        Thumbnail = videoData.Thumbnail;
        Chapters = videoData.Chapters?.Select(c => new YouTubeChapter {
            StartTime = c.StartTime ?? 0,
            EndTime = c.EndTime ?? 0,
            Title = c.Title
        }).ToList() ?? [];
    }

    void FillWithFindYoutubeVideo() {
        var fybResponse = HttpClient.Call(new FindYoutubeVideoRpc(Id.Checked()));
        var archiveItem =
            fybResponse?.Keys.FirstOrDefault(key
                => key.Archived && key.Name == "Archive.org Details");
        if (archiveItem == null) {
            throw new DataNotFoundException(
                $"Cannot find video history with FindYoutubeVideo service: {fybResponse.ToJson()}");
        }

        var archiveLink = archiveItem.Available.FirstOrDefault(link => link.Url != null)?.Url;

        if (archiveLink == null) {
            throw new DataNotFoundException(
                $"Cannot find link with FindYoutubeVideo service: {fybResponse.ToJson()}");
        }

        var archiveId = archiveLink.Split("/").Last();
        var archiveMetadata = HttpClient.Call(new ArchiveMetadataRpc(archiveId));
        if (archiveMetadata == null) {
            throw new DataNotFoundException($"Cannot find archive for {archiveId}");
        }

        var archiveFile =
            archiveMetadata.Files.FirstOrDefault(f => f.Name.EndsWith($"-{Id}.info.json"));

        if (archiveFile == null) {
            throw new DataNotFoundException($"Cannot find item {Id} in archive {archiveId}");
        }

        var archiveFileContent = HttpClient.Call(new ArchiveItemDetailRpc(archiveMetadata.D1,
            archiveMetadata.Dir, archiveFile.Name));

        if (archiveFileContent == null) {
            throw new DataNotFoundException(
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
        if (cdxResults == null) {
            return false;
        }

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
