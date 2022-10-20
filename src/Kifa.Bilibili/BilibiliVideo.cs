using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Bilibili.BiliplusApi;
using Kifa.IO;
using Kifa.Service;
using Kifa.Subtitle.Ass;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliVideoStats {
    public long PlayCount { get; set; }
    public long DanmakuCount { get; set; }
    public long CoinCount { get; set; }
    public long LikeCount { get; set; }
    public long FavoriteCount { get; set; }
    public long ReplyCount { get; set; }
    public long ShareCount { get; set; }
}

public class BilibiliVideo : DataModel<BilibiliVideo> {
    public const string ModelId = "bilibili/videos";

    public enum PartModeType {
        SinglePartMode,
        ContinuousPartMode,
        ParallelPartMode
    }

    enum DownloadStatus {
        NoAccess,
        CanAdd,
        InProgress,
        Done
    }

    static KifaServiceClient<BilibiliVideo>? client;

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static HttpClient bilibiliClient;

    PartModeType partMode;

    public static KifaServiceClient<BilibiliVideo> Client
        => client ??= new KifaServiceRestClient<BilibiliVideo>();

    public static string BilibiliCookies { get; set; }

    const int DefaultCodec = 7;

    static readonly List<int> DesiredCodecs = new() {
        12,
        7
    };

    static readonly Dictionary<int, string> CodecNames = new() {
        { 7, "avc" },
        { 12, "hevc" },
        { 13, "av1" }
    };

    public static int BlockSize { get; set; } = 32 << 20;

    public static int ThreadCount { get; set; } = 1;

    public string Title { get; set; }
    public string Author { get; set; }
    public string AuthorId { get; set; }
    public string Description { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Category { get; set; }
    public Uri Cover { get; set; }
    public DateTimeOffset? Uploaded { get; set; }
    public List<BilibiliChat> Pages { get; set; }
    public BilibiliVideoStats Stats { get; set; } = new();

    [JsonIgnore]
    public PartModeType PartMode {
        get => partMode;
        set {
            partMode = value;
            if (partMode == PartModeType.ContinuousPartMode) {
                var offset = TimeSpan.Zero;
                foreach (var part in Pages) {
                    part.ChatOffset = offset;
                    offset += part.Duration;
                }
            } else {
                foreach (var part in Pages) {
                    part.ChatOffset = TimeSpan.Zero;
                }
            }
        }
    }

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        try {
            if (FillWithBilibili()) {
                return Date.Zero;
            }

            Logger.Debug($"Unable to find video {Id} from bilibili API.");
        } catch (Exception e) {
            Logger.Debug(e, $"Unable to find video {Id} from bilibili API.");
        }

        try {
            if (FillWithBiliplus()) {
                return Date.Zero;
            }

            Logger.Debug($"Unable to find video {Id} from biliplus API.");
        } catch (Exception e) {
            Logger.Debug(e, $"Unable to find video {Id} from biliplus API.");
        }

        try {
            if (!FillWithBiliplusCache()) {
                Logger.Debug($"Unable to find video {Id} from biliplus cache.");
            }
        } catch (Exception e) {
            Logger.Debug(e, $"Unable to find video {Id} from biliplus cache.");
        }

        return Date.Zero;
    }

    bool FillWithBilibili() {
        var data = new VideoRpc().Invoke(Id).Data;
        var tags = new VideoTagRpc().Invoke(Id).Data;
        Title = data.Title;
        Author = data.Owner.Name;
        AuthorId = data.Owner.Mid.ToString();
        Description = data.Desc;
        Tags = tags.Select(t => t.TagName).OrderBy(t => t).ToList();
        Category = data.Tname;
        Cover = data.Pic;
        Pages = data.Pages.Select(p => new BilibiliChat {
            Id = p.Page,
            Cid = p.Cid.ToString(),
            Title = p.Part,
            Duration = TimeSpan.FromSeconds(p.Duration)
        }).ToList();
        Uploaded = DateTimeOffset.FromUnixTimeSeconds(data.Pubdate);
        Uploaded = Uploaded.Value.ToOffset(TimeZones.ShanghaiTimeZone.GetUtcOffset(Uploaded.Value));

        Height = data.Dimension.Height;
        Width = data.Dimension.Width;

        var stat = data.Stat;
        Stats.PlayCount = stat.View;
        Stats.DanmakuCount = stat.Danmaku;
        Stats.CoinCount = stat.Coin;
        Stats.FavoriteCount = stat.Favorite;
        Stats.ReplyCount = stat.Reply;
        Stats.ShareCount = stat.Share;
        Stats.LikeCount = stat.Like;

        return true;
    }

    bool FillWithBiliplus() {
        var data = new BiliplusVideoRpc().Invoke(Id);
        if (data == null) {
            Logger.Error("Failed to retrieve data for video (Id) from biliplus.");
            return false;
        }

        var v2 = data.V2AppApi;

        if (data.Title == null && v2 == null) {
            return false;
        }

        if (v2 != null) {
            Title = v2.Title;
            Author = v2.Owner.Name;
            AuthorId = v2.Owner.Mid.ToString();
            Description = v2.Desc;
            if (v2.Dimension != null) {
                Width = v2.Dimension.Width;
                Height = v2.Dimension.Height;
            }

            if (v2.Tag != null) {
                Tags.AddRange(v2.Tag.Select(t => t.TagName));
            }

            Category = v2.Tname;
            Cover = v2.Pic;
            Pages = v2.Pages.Select(p => new BilibiliChat {
                Id = p.Page,
                Cid = p.Cid.ToString(),
                Title = p.Part,
                Duration = TimeSpan.FromSeconds(p.Duration)
            }).ToList();
            Uploaded = DateTimeOffset.FromUnixTimeSeconds(v2.Pubdate);
            Uploaded =
                Uploaded.Value.ToOffset(TimeZones.ShanghaiTimeZone.GetUtcOffset(Uploaded.Value));

            var stat = v2.Stat;
            Stats.PlayCount = stat.View;
            Stats.DanmakuCount = stat.Danmaku;
            Stats.CoinCount = stat.Coin;
            Stats.LikeCount = stat.Like;
            Stats.FavoriteCount = stat.Favorite;
            Stats.ReplyCount = stat.Reply;
            Stats.ShareCount = stat.Share;
        } else {
            Title = data.Title!;
            Author = data.Author;
            AuthorId = data.Mid.ToString();
            Description = data.Description;
            Tags = data.Tag.Split(",").ToList();
            Category = data.Typename;
            Cover = data.Pic;
            Pages = data.List.Select(p => new BilibiliChat {
                Id = p.Page,
                Cid = p.Cid.ToString(),
                Title = p.Part
            }).ToList();
            Uploaded = DateTimeOffset.FromUnixTimeSeconds(v2.Pubdate);
            Uploaded =
                Uploaded.Value.ToOffset(TimeZones.ShanghaiTimeZone.GetUtcOffset(Uploaded.Value));

            Stats.PlayCount = data.Play;
            Stats.DanmakuCount = data.VideoReview;
            Stats.CoinCount = data.Coins;
            Stats.FavoriteCount = data.Favorites;
            Stats.ReplyCount = data.Review;
        }

        return true;
    }

    bool FillWithBiliplusCache() {
        var data = new BiliplusVideoCacheRpc().Invoke(Id).Data;
        var info = data.Info;
        Title = info.Title;
        Author = info.Author;
        AuthorId = info.Mid.ToString();
        Description = info.Description;
        Tags = info.Keywords.Split(",").ToList();
        Category = info.Typename;
        Cover = info.Pic;
        Pages = data.Parts.Select(p => new BilibiliChat {
            Id = p.Page,
            Cid = p.Cid.ToString(),
            Title = p.Part
        }).ToList();
        Uploaded = info.Create.ParseDateTimeOffset(TimeZones.ShanghaiTimeZone);

        Stats.PlayCount = info.Play;
        Stats.DanmakuCount = info.VideoReview;
        Stats.CoinCount = info.Coins;
        Stats.FavoriteCount = info.Favorites;
        Stats.ReplyCount = info.Review;

        return true;
    }

    public AssDocument GenerateAssDocument() {
        var result = new AssDocument();
        result.Sections.Add(new AssScriptInfoSection {
            Title = Title,
            OriginalScript = "Bilibili"
        });
        result.Sections.Add(new AssStylesSection {
            Styles = new List<AssStyle> {
                AssStyle.DefaultStyle
            }
        });
        var events = new AssEventsSection();
        result.Sections.Add(events);

        foreach (var part in Pages)
        foreach (var comment in part.Comments) {
            events.Events.Add(comment.GenerateAssDialogue());
        }

        return result;
    }

    static readonly Regex FileNamePattern =
        new(@"[-./](av\d+)(p\d+)?\.(c\d+)\.(\d+)(?:-(\w+))?.mp4");

    public static (BilibiliVideo? video, int pid, int quality, int codec) Parse(string file) {
        var match = FileNamePattern.Match(file);
        if (!match.Success) {
            return (null, 1, 0, DefaultCodec);
        }

        return (Client.Get(match.Groups[1].Value),
            match.Groups[2].Success ? int.Parse(match.Groups[2].Value[1..]) : 1,
            match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0,
            match.Groups[5].Success
                ? CodecNames.First(c => c.Value == match.Groups[5].Value).Key
                : DefaultCodec);
    }

    public List<string> GetCanonicalNames(int pid, int quality, int codec) {
        var p = Pages.First(x => x.Id == pid);

        return new List<string> {
            $"$/{GetSuffix(Id, pid, p.Cid, quality, codec)}",
            $"$/{GetSuffix(null, pid, p.Cid, quality, codec)}"
        };
    }

    public string GetDesiredName(int pid, int quality, int codec, string? alternativeFolder = null,
        bool prefixDate = false, BilibiliUploader? uploader = null) {
        var p = Pages.First(x => x.Id == pid);

        var partName = p.Title.NormalizeFileName();
        var title = Title.NormalizeFileName();
        if (title.Contains(partName)) {
            partName = "";
        } else if (partName.StartsWith(title)) {
            partName = partName[title.Length..].Trim();
        }

        var prefix = prefixDate ? $"{Uploaded.Value:yyyy-MM-dd}" : "";
        var pidText = $"P{pid.ToString("D" + Pages.Count.ToString().Length)}";

        uploader ??= new BilibiliUploader {
            Id = AuthorId,
            Name = Author
        };

        var folder = alternativeFolder ?? $"{uploader.Name}-{uploader.Id}".NormalizeFileName();
        var partString = Pages.Count > 1 ? $"{pidText} {partName}" : partName;
        return
            $"{folder}/{$"{prefix} {title} {partString}".NormalizeFileName()}.{GetSuffix(Id, pid, p.Cid, quality, codec)}";
    }

    static string GetSuffix(string? aid, int pid, string cid, int quality, int codec) {
        var codecString = codec == DefaultCodec ? "" : $"-{CodecNames[codec]}";
        var aidString = aid == null ? "" : $"{aid}p{pid}.";
        return $"{aidString}c{cid}.{quality}-{codecString}";
    }

    public (string extension, int quality, int codec, Func<Stream> videoStreamGetter,
        List<Func<Stream>> audioStreamGetters) GetStreams(int pid) {
        var cid = Pages[pid - 1].Cid;

        var (extension, quality, codec, videoLink, audioLinks) = GetDownloadLinks(Id, cid);
        return (extension, quality, codec, () => BuildDownloadStream(videoLink),
            audioLinks.Select<(List<string> links, long size), Func<Stream>>(l
                => () => BuildDownloadStream(l)).ToList());
    }

    static Stream BuildDownloadStream((List<string> links, long size) link) {
        string? finalLink = null;
        long? length = null;
        foreach (var l in link.links) {
            try {
                length = GetBilibiliClient().GetContentLength(l);
            } catch (HttpRequestException ex) {
                Logger.Warn(ex, $"Not available: {l}");
                continue;
            }

            if (length == null) {
                Logger.Warn($"Length not found: {l}");
            }

            if (length * 2 < link.size) {
                throw new FileCorruptedException(
                    $"Content length ({length}) is too much smaller than ({link.size}).");
            }

            finalLink = l;
            break;
        }

        if (finalLink == null) {
            throw new Exception("No suitable download link.");
        }

        return new SeekableReadStream(length.Value, (buffer, bufferOffset, offset, count) => {
            if (count < 0) {
                count = buffer.Length - bufferOffset;
            }

            Logger.Trace($"Downloading from {offset} to {offset + count} of {finalLink}...");

            return Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Get, finalLink);

                request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                using var response = GetBilibiliClient().SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
                return (int) memoryStream.Position;
            }, (ex, i) => {
                if (i >= 5) {
                    throw ex;
                }

                Logger.Warn(ex, $"Download from {offset} to {offset + count} failed ({i})...");
                Thread.Sleep(TimeSpan.FromSeconds(30));
            });
        }, BlockSize, ThreadCount);
    }

    static void AddDownloadJob(string aid) {
        var response = BiliplusHttpClient.Instance
            .GetAsync($"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&checkall")
            .Result;
        var content = response.GetString();
        Logger.Debug($"Add download request result: {content}");
    }

    static void UpdateDownloadStatus(string cid) {
        var response = BiliplusHttpClient.Instance
            .GetAsync($"https://bg.biliplus-vid.top/api/saver_status.php?cid={cid}").Result;
        var content = response.GetString();
        Logger.Debug($"Check saver status: {content}");
    }

    static DownloadStatus GetDownloadStatus(string aid, int pid) {
        var response = BiliplusHttpClient.Instance
            .GetAsync(
                $"https://www.biliplus.com/api/geturl?bangumi=0&av={aid.Substring(2)}&page={pid}")
            .Result;
        var content = response.GetString();
        Logger.Debug($"Get download result: {content}");
        var storage = JToken.Parse(content)["storage"];
        var access = (int) storage["access"];
        switch (access) {
            case 0:
                return DownloadStatus.NoAccess;
            case 1 when (bool) storage["inProgress"]:
                return DownloadStatus.InProgress;
            case 1 when (bool) storage["canAdd"]:
                return DownloadStatus.CanAdd;
            case 2:
                return DownloadStatus.Done;
            default:
                throw new Exception("Unexpected download status");
        }
    }

    static (string extension, int quality, int codec, (List<string> links, long size) videoLink,
        List<(List<string> links, long size)> audioLinks) GetDownloadLinks(string aid, string cid) {
        var quality = 127;
        while (true) {
            var response = GetBilibiliClient()
                .SendWithRetry<VideoUrlResponse>(new VideoUrlRequest(aid, cid, quality));

            if (response is not { Code: 0 }) {
                Logger.Warn($"bilibili API error: {response?.Message} ({response?.Code}).");
                Thread.Sleep(TimeSpan.FromSeconds(10));
                continue;
            }

            var data = response.Data!;
            var receivedQuality = data.Quality;
            if (receivedQuality != data.AcceptQuality[0]) {
                quality = data.AcceptQuality[0];
                Logger.Warn($"Quality mismatch: received quality {receivedQuality}, " +
                            $"best quality {quality}.");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                continue;
            }

            var videos = data.Dash.Video.Where(v => v.Id == receivedQuality).ToList();
            var codec = DesiredCodecs.FirstOrDefault(c => videos.Any(v => v.Codecid == c));
            if (codec == 0) {
                Logger.Warn(
                    $"No desired code found: expected {string.Join(", ", DesiredCodecs)}, found {string.Join(", ", videos.Select(v => v.Codecid))}");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                continue;
            }

            var video = videos.First(v => v.Codecid == codec);
            var audio = data.Dash.Audio![0];
            var dolby = data.Dash.Dolby?.Audio?[0];
            var flac = data.Dash.Flac?.Audio?[0];
            var audios = new List<Resource>();
            if (dolby != null) {
                audios.Add(dolby);
            }

            if (flac != null) {
                audios.Add(flac);
            }

            audios.Add(audio);

            // dvh seems to calculate the bandwidth incorrectly.
            var videoSize = video.Codecs.StartsWith("dvh")
                ? video.Bandwidth * data.Dash.Duration / 8 / 5
                : video.Bandwidth * data.Dash.Duration / 8;

            return (video.MimeType.Split("/").Last(), receivedQuality, codec,
                ((video.BackupUrl ?? Enumerable.Empty<string>()).Prepend(video.BaseUrl).ToList(),
                    videoSize),
                audios.Select(audio => (
                    (audio.BackupUrl ?? Enumerable.Empty<string>()).Prepend(audio.BaseUrl).ToList(),
                    audio.Bandwidth * data.Dash.Duration / 8)).ToList());
        }
    }

    static string GetDownloadPage(string cid) {
        var response = BiliplusHttpClient.Instance
            .GetAsync($"https://www.biliplus.com/api/video_playurl?cid={cid}&type=mp4").Result;
        var content = response.GetString();
        Logger.Debug($"Downloaded page content: {content}");

        return content;
    }

    public static string? GetAid(string cid) {
        var response = BiliplusHttpClient.Instance
            .GetAsync($"https://www.biliplus.com/api/cidinfo?cid={cid}").Result;
        var content = response.GetString();
        Logger.Debug($"Cid info: {content}");

        var data = JToken.Parse(content)["data"];
        if (data == null || data["aid"] == null || data["page"] == null) {
            Logger.Debug($"Failed to retrieve cid info");
            return null;
        }

        return $"av{data["aid"]}p{data["page"]}";
    }

    public static HttpClient GetBilibiliClient() {
        if (bilibiliClient == null) {
            bilibiliClient = new HttpClient {
                Timeout = TimeSpan.FromMinutes(10)
            };
            bilibiliClient.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
            bilibiliClient.DefaultRequestHeaders.Referrer = new Uri("https://space.bilibili.com/");
            bilibiliClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
        }

        return bilibiliClient;
    }
}
