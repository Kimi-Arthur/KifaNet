using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using HtmlAgilityPack;
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

    public static bool UseMergedSource { get; set; }

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

    static KifaServiceClient<BilibiliVideo> client;

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static HttpClient bilibiliClient;

    PartModeType partMode;

    public static KifaServiceClient<BilibiliVideo> Client
        => client ??= new KifaServiceRestClient<BilibiliVideo>();

    public static string BilibiliCookies { get; set; }

    public static int DefaultBiliplusSourceChoice { get; set; }

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

    public string GetCanonicalName(int pid, int quality, string? cid = null) {
        var p = Pages.First(x => x.Id == pid);

        if (cid != null && cid != p.Cid) {
            return null;
        }

        return $"$/{Id}p{pid}.c{p.Cid}.{quality}";
    }

    public string GetDesiredName(int pid, int quality, string? cid = null,
        string? alternativeFolder = null, bool prefixDate = false,
        BilibiliUploader? uploader = null) {
        var p = Pages.First(x => x.Id == pid);

        if (cid != null && cid != p.Cid) {
            return null;
        }

        var partName = p.Title.NormalizeFileName();
        var title = Title.NormalizeFileName();
        if (title.Contains(partName)) {
            partName = "";
        } else if (partName.StartsWith(title)) {
            partName = partName.Substring(title.Length);
        }

        var prefix = prefixDate ? $"{Uploaded.Value:yyyy-MM-dd}" : "";
        var pidText = $"P{pid.ToString("D" + Pages.Count.ToString().Length)}";

        uploader ??= new BilibiliUploader {
            Id = AuthorId,
            Name = Author
        };

        return (alternativeFolder == null
            ? $"{uploader.Name}-{uploader.Id}".NormalizeFileName()
            : $"{alternativeFolder}") + (Pages.Count > 1
            ? $"/{$"{prefix} {title} {pidText} {partName}".NormalizeFileName()}-{Id}p{pid}.c{p.Cid}.{quality}"
            : $"/{$"{prefix} {title} {partName}".NormalizeFileName()}-{Id}.c{p.Cid}.{quality}");
    }

    public (string extension, int quality, List<Func<Stream>> streamGetters) GetVideoStreams(
        int pid, int biliplusSourceChoice = 0) {
        var cid = Pages[pid - 1].Cid;

        if (UseMergedSource) {
            AddDownloadJob(Id);

            while (GetDownloadStatus(Id, pid) == DownloadStatus.InProgress) {
                Logger.Debug("Download not ready. Sleep 30 seconds...");
                UpdateDownloadStatus(cid);
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(GetDownloadPage(cid));

            var choices = doc.DocumentNode.SelectNodes("//a")?.Select(linkNode
                => (name: linkNode.InnerText, link: linkNode.Attributes["href"].Value)).ToList();

            if (choices == null) {
                Logger.Warn("No sources found. Job not successful?");
                return (null, -1, null);
            }

            var initialSource = biliplusSourceChoice;
            while (true) {
                try {
                    Logger.Debug("Choosen source: " +
                                 $"{choices[biliplusSourceChoice].name}({choices[biliplusSourceChoice].link})");
                    var link = choices[biliplusSourceChoice].link;
                    return ("mp4", -1, new List<Func<Stream>> {
                        () => BuildDownloadStream(link)
                    });
                } catch (Exception ex) {
                    biliplusSourceChoice = (biliplusSourceChoice + 1) % choices.Count;
                    if (biliplusSourceChoice == initialSource) {
                        throw;
                    }

                    Logger.Warn(ex, "Download failed. Try next source.");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
            }
        } else {
            var (extension, quality, links) = GetDownloadLinks(Id, cid);
            return extension == null
                ? (null, -1, null)
                : (extension, quality,
                    links.Select<string, Func<Stream>>(l => () => BuildDownloadStream(l)).ToList());
        }
    }

    static Stream BuildDownloadStream(string link) {
        var length = GetBilibiliClient().GetContentLength(link);
        if (length == null) {
            throw new Exception("Content length is not found.");
        }

        return new SeekableReadStream(length.Value, (buffer, bufferOffset, offset, count) => {
            if (count < 0) {
                count = buffer.Length - bufferOffset;
            }

            Logger.Trace($"Downloading from {offset} to {offset + count} of {link}...");

            return Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Get, link);

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
        using var response = BiliplusHttpClient.Instance
            .GetAsync($"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&checkall")
            .Result;
        var content = response.GetString();
        Logger.Debug($"Add download request result: {content}");
    }

    static void UpdateDownloadStatus(string cid) {
        using var response = BiliplusHttpClient.Instance
            .GetAsync($"https://bg.biliplus-vid.top/api/saver_status.php?cid={cid}").Result;
        var content = response.GetString();
        Logger.Debug($"Check saver status: {content}");
    }

    static DownloadStatus GetDownloadStatus(string aid, int pid) {
        using var response = BiliplusHttpClient.Instance
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

    static (string extension, int quality, List<string> links) GetDownloadLinks(string aid,
        string cid) {
        var quality = 120;
        while (true) {
            using var response = GetBilibiliClient()
                .GetAsync(
                    $"https://api.bilibili.com/x/player/playurl?cid={cid}&avid={aid.Substring(2)}&qn={quality}&fourk=1")
                .Result;
            var content = response.GetString();
            Logger.Debug($"Get download result: {content}");
            var data = JToken.Parse(content);
            if ((int) data["code"] != 0) {
                Logger.Warn($"bilibili API error: {data["message"]} ({data["code"]}).");
                Thread.Sleep(TimeSpan.FromSeconds(10));
                continue;
            }

            var receivedQuality = (int) data["data"]["quality"];
            if (receivedQuality != (int) data["data"]["accept_quality"][0]) {
                quality = (int) data["data"]["accept_quality"][0];
                Logger.Warn($"Quality mismatch: received quality {receivedQuality}, " +
                            $"best quality {data["data"]["accept_quality"][0]}.");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                continue;
            }

            var urls = data["data"]["durl"];
            var extension = (string) urls[0]["url"];
            extension = extension[..extension.IndexOf('?')];
            extension = extension[(extension.LastIndexOf('.') + 1)..];
            return (extension, receivedQuality, urls.Select(x => (string) x["url"]).ToList());
        }
    }

    static string GetDownloadPage(string cid) {
        using var response = BiliplusHttpClient.Instance
            .GetAsync($"https://www.biliplus.com/api/video_playurl?cid={cid}&type=mp4").Result;
        var content = response.GetString();
        Logger.Debug($"Downloaded page content: {content}");

        return content;
    }

    public static string? GetAid(string cid) {
        using var response = BiliplusHttpClient.Instance
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
