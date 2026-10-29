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
    public double CoinCount { get; set; }
    public long LikeCount { get; set; }
    public long FavoriteCount { get; set; }
    public long ReplyCount { get; set; }
    public long ShareCount { get; set; }
}

public class BilibiliVideo : DataModel, WithModelId<BilibiliVideo> {
    public static string ModelId => "bilibili/videos";

    public static KifaServiceClient<BilibiliVideo> Client { get; set; } =
        new KifaServiceRestClient<BilibiliVideo>();

    public static List<string> IgnoredDomains { get; set; } = ["akamaized.net"];

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

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    PartModeType partMode;

    const int MissingCodec = 0;
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
    public List<string> Tags { get; set; } = [];
    public string Category { get; set; }
    public Uri Cover { get; set; }
    public DateTimeOffset? Uploaded { get; set; }
    public List<BilibiliChat> Pages { get; set; } = [];
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


    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(365);

    public override DataVersion? ForceRefreshBefore => new(2026, 10, 29);

    public override void Fill(bool deep = false) {
        try {
            FillWithBilibili();
            return;
        } catch (BilibiliVideoNotFoundException) {
            Logger.Debug($"Video {Id} not found from bilibili API.");
        } catch (Exception e) {
            Logger.Warn(e, $"Failed to fill video {Id} from bilibili API due to unexpected or transient error.");
            throw new FailedToFillException($"Failed to fill video {Id} from bilibili API.", e);
        }

        try {
            if (FillWithBiliplus()) {
                return;
            }

            Logger.Debug($"Unable to find video {Id} from biliplus API.");
        } catch (Exception e) {
            Logger.Debug(e, $"Unable to find video {Id} from biliplus API.");
        }

        try {
            FillWithBiliplusCache();
            return;
        } catch (Exception e) {
            Logger.Debug(e, $"Unable to find video {Id} from biliplus cache.");
        }

        throw new DataNotFoundException();
    }

    public static readonly Dictionary<long, string> KnownCategories = new() {
        { 1, "动画" },
        { 24, "MAD·AMV" },
        { 25, "MMD·3D" },
        { 47, "短片·手书·配音" },
        { 86, "特摄" },
        { 210, "手办·模玩" },
        { 258, "动漫杂谈" },
        { 27, "动画综合" },
        { 13, "番剧" },
        { 33, "连载动画" },
        { 32, "完结动画" },
        { 51, "资讯" },
        { 152, "官方延伸" },
        { 168, "国创" },
        { 153, "国产动画" },
        { 169, "国产原创相关" },
        { 195, "布袋戏" },
        { 170, "资讯" },
        { 3, "音乐" },
        { 28, "原创音乐" },
        { 31, "翻唱" },
        { 59, "演奏" },
        { 30, "VOCALOID·UTAU" },
        { 194, "电音" },
        { 193, "MV" },
        { 29, "音乐现场" },
        { 130, "音乐综合" },
        { 243, "乐评盘点" },
        { 244, "音乐教学" },
        { 129, "舞蹈" },
        { 20, "宅舞" },
        { 198, "街舞" },
        { 199, "明星舞蹈" },
        { 200, "中国舞" },
        { 257, "舞蹈综合" },
        { 154, "舞蹈教程" },
        { 4, "游戏" },
        { 17, "单机游戏" },
        { 171, "电子竞技" },
        { 172, "手机游戏" },
        { 65, "网络游戏" },
        { 173, "桌游棋牌" },
        { 121, "GMV" },
        { 136, "音游" },
        { 19, "Mugen" },
        { 36, "知识" },
        { 201, "科学科普" },
        { 124, "社科·法律·心理" },
        { 228, "人文历史" },
        { 207, "财经商业" },
        { 208, "校园学习" },
        { 209, "职业职场" },
        { 229, "设计·创意" },
        { 122, "野生技术协会" },
        { 188, "科技" },
        { 95, "数码" },
        { 230, "软件应用" },
        { 231, "计算机技术" },
        { 232, "工业·工程·机械" },
        { 202, "极客DIY" },
        { 234, "运动" },
        { 235, "篮球" },
        { 249, "足球" },
        { 250, "健身" },
        { 251, "竞技体育" },
        { 252, "运动综合" },
        { 223, "汽车" },
        { 176, "汽车生活" },
        { 224, "汽车文化" },
        { 225, "汽车极客" },
        { 226, "摩托车" },
        { 227, "智能出行" },
        { 160, "生活" },
        { 138, "搞笑" },
        { 253, "出行" },
        { 254, "三农" },
        { 255, "家居房产" },
        { 248, "手工" },
        { 21, "日常" },
        { 211, "美食" },
        { 76, "美食制作" },
        { 212, "美食侦探" },
        { 213, "美食测评" },
        { 214, "田园美食" },
        { 215, "美食记录" },
        { 217, "动物圈" },
        { 218, "喵星人" },
        { 219, "汪星人" },
        { 220, "大熊猫" },
        { 221, "野生动物" },
        { 222, "爬宠" },
        { 260, "动物综合" },
        { 119, "鬼畜" },
        { 22, "鬼畜调教" },
        { 26, "音MAD" },
        { 126, "人力VOCALOID" },
        { 216, "鬼畜剧场" },
        { 127, "教程演示" },
        { 155, "时尚" },
        { 157, "美妆护肤" },
        { 158, "穿搭" },
        { 159, "时尚潮流" },
        { 5, "娱乐" },
        { 71, "综艺" },
        { 241, "娱乐杂谈" },
        { 242, "粉丝创作" },
        { 137, "明星综合" },
        { 181, "影视" },
        { 182, "影视剪辑" },
        { 183, "影视杂谈" },
        { 85, "小剧场" },
        { 184, "预告·资讯" },
        { 177, "纪录片" },
        { 37, "人文·历史" },
        { 178, "科学·探索·自然" },
        { 179, "军事" },
        { 180, "社会·美食·旅行" },
        { 23, "电影" },
        { 147, "华语电影" },
        { 145, "欧美电影" },
        { 146, "日本电影" },
        { 83, "其他国家" },
        { 11, "电视剧" },
        { 185, "国产剧" },
        { 187, "海外剧" }
    };

    static string ResolveCategory(string? tname, string? tnameV2, long tid, long tidV2 = 0) {
        if (!string.IsNullOrEmpty(tname)) {
            return tname;
        }

        if (!string.IsNullOrEmpty(tnameV2)) {
            return tnameV2;
        }

        return KnownCategories.GetValueOrDefault(tid, KnownCategories.GetValueOrDefault(tidV2, ""));
    }

    void FillWithBilibili() {
        var client = HttpClients.GetBilibiliClient();
        var response = client.Call(new VideoRpc(Id));
        if (response?.Code == -404) {
            Logger.Warn("Fail to locate video with direct access. Retry with CN access");
            response = HttpClients.GetBilibiliClient(BilibiliRegion.Cn).Call(new VideoRpc(Id));
        }

        if (response?.Code is -404 or 62002 or 62004) {
            throw new BilibiliVideoNotFoundException(
                $"Video {Id} not found on Bilibili ({response.Code}: {response.Message}).");
        }

        if (response == null || response.Code != 0) {
            throw new FailedToFillException(
                $"Bilibili API returned error for {Id} ({response?.Code}: {response?.Message}).");
        }

        var data = response.Data.Checked();

        List<string>? tags = null;
        try {
            tags = client.Call(new VideoTagRpc(Id))?.Data?.Select(t => t.TagName).OrderBy(t => t).ToList();
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to retrieve tags for video {Id}. Keeping existing tags.");
        }

        Title = data.Title;
        Author = data.Owner.Name;
        AuthorId = data.Owner.Mid.ToString();
        Description = data.Desc;
        if (tags != null) {
            Tags = tags;
        }
        Category = ResolveCategory(data.Tname, data.TnameV2, data.Tid, data.TidV2);
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
    }

    bool FillWithBiliplus() {
        var data = HttpClients.BiliplusHttpClient.Call(new BiliplusVideoRpc(Id));
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

            Category = ResolveCategory(v2.Tname, null, v2.Tid);
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
            Category = ResolveCategory(data.Typename, null, data.Tid);
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

    void FillWithBiliplusCache() {
        var data = HttpClients.BiliplusHttpClient.Call(new BiliplusVideoCacheRpc(Id))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Failed to get cache for {Id}.");
        }

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

    public static (string? Aid, int Pid, int Quality, int Codec) ParseParts(string file) {
        var match = FileNamePattern.Match(file);
        if (!match.Success) {
            return (null, 1, 0, DefaultCodec);
        }

        return (match.Groups[1].Value,
            match.Groups[2].Success ? int.Parse(match.Groups[2].Value[1..]) : 1,
            match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0,
            match.Groups[5].Success ? GetCodecId(match.Groups[5].Value) : DefaultCodec);
    }

    public static (BilibiliVideo? Video, int Pid, int Quality, int Codec) Parse(string file) {
        var parts = ParseParts(file);
        return (parts.Aid != null ? Client.Get(parts.Aid) : null, parts.Pid, parts.Quality,
            parts.Codec);
    }

    public List<string> GetCanonicalNames(int pid, int quality, int codec) {
        var p = Pages.First(x => x.Id == pid);

        return new List<string> {
            $"{GetSuffix(Id, pid, p.Cid, quality, codec)}",
            $"{GetSuffix(null, pid, p.Cid, quality, codec)}"
        };
    }

    // Common file extension suffix (e.g., ".mp4") length.
    const int TypeSuffixLength = 4;

    public string GetDesiredName(int pid, int quality, int codec, bool includePageTitle,
        string? alternativeFolder = null, string? extraFolder = null, string? prefix = null,
        BilibiliUploader? uploader = null, bool includeUploaderInFileTitle = false,
        bool limitFileLength = false) {
        var title = Title.NormalizeFileName();

        uploader ??= new BilibiliUploader {
            Id = AuthorId,
            Name = Author
        };

        var uploaderName = uploader.Name.NormalizeFileName();

        var folder = alternativeFolder != null
            ? $"{alternativeFolder.Split('/')[0].Choppable()}.bilibili/{alternativeFolder.Split('/').Skip(1).JoinBy('/')}".TrimEnd('/')
            : $"{uploaderName.Choppable()}.{uploader.Id}.bilibili";

        if (extraFolder != null) {
            folder = $"{folder}/{extraFolder}";
        }

        var filenameSegments = new List<string>();
        if (prefix != null) {
            filenameSegments.Add(prefix.NormalizeFileName());
        }

        if (includeUploaderInFileTitle) {
            filenameSegments.Add($"[{uploaderName}.{uploader.Id}]");
        }

        filenameSegments.Add(title);

        var p = Pages.First(x => x.Id == pid);

        if (Pages.Count > 1) {
            filenameSegments.Add($"P{pid.ToString("D" + Pages.Count.ToString().Length)}");
        }

        var partName = p.Title.NormalizeFileName();
        if (includePageTitle && !title.Contains(partName)) {
            if (partName.StartsWith(title)) {
                partName = partName[title.Length..].Trim();
            }

            filenameSegments.Add(partName);
        }

        var suffix = GetSuffix(Id, pid, p.Cid, quality, codec);
        return $"{folder}/{filenameSegments.JoinBy(" ").Choppable()}.{suffix}".NormalizeFilePath(TypeSuffixLength);
    }

    static string GetSuffix(string? aid, int pid, string cid, int quality, int codec) {
        var codecString = codec is DefaultCodec or MissingCodec ? "" : $"-{CodecNames[codec]}";
        var aidString = aid == null ? "" : $"{aid}p{pid}.";
        return $"{aidString}c{cid}.{quality}{codecString}";
    }

    public (string extension, int quality, int codec, Func<Stream> videoStreamGetter,
        List<Func<Stream>> audioStreamGetters) GetStreams(int pid, int maxQuality = 0,
            string? preferredCodec = null, BilibiliRegion region = BilibiliRegion.Direct) {
        var cid = Pages[pid - 1].Cid;

        var (extension, quality, codec, videoLink, audioLinks) = GetDownloadLinks(Id, cid,
            maxQuality: maxQuality == 0 ? 127 : maxQuality, preferredCodec, region: region);
        return (extension, quality, codec, () => BuildDownloadStream(videoLink),
            audioLinks.Select<(List<string> links, long size), Func<Stream>>(l
                => () => BuildDownloadStream(l)).ToList());
    }

    static Stream BuildDownloadStream((List<string> links, long size) link) {
        string? finalLink = null;
        long? length = null;
        foreach (var l in link.links) {
            try {
                length = HttpClients.GetBilibiliClient().GetContentLength(l);
            } catch (HttpRequestException ex) {
                Logger.Warn(ex, $"Not available: {l}");
                continue;
            }

            if (length == null) {
                Logger.Warn($"Length not found: {l}");
            }

            if (length * 2 < link.size) {
                Logger.Warn($"Content length ({length}) is too much smaller than ({link.size}).");
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
                using var response = HttpClients.GetBilibiliClient().SendAsync(request).Result;
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
        var response = HttpClients.BiliplusHttpClient
            .GetAsync($"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&checkall")
            .Result;
        var content = response.GetString();
        Logger.Debug($"Add download request result: {content}");
    }

    static void UpdateDownloadStatus(string cid) {
        var response = HttpClients.BiliplusHttpClient
            .GetAsync($"https://bg.biliplus-vid.top/api/saver_status.php?cid={cid}").Result;
        var content = response.GetString();
        Logger.Debug($"Check saver status: {content}");
    }

    static DownloadStatus GetDownloadStatus(string aid, int pid) {
        var response = HttpClients.BiliplusHttpClient
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
        List<(List<string> links, long size)> audioLinks) GetDownloadLinks(string aid, string cid,
            int maxQuality, string? preferredCodec, BilibiliRegion region) {
        var quality = maxQuality;
        return Retry.Run(() => {
            var response = HttpClients.GetBilibiliClient(region)
                .Call(new VideoUrlRpc(aid, cid, quality));

            if (response is not { Code: 0 }) {
                if (response.Code == -404) {
                    throw new BilibiliVideoNotFoundException(
                        $"{response.Message} ({response.Code})");
                }

                throw new Exception($"bilibili API error: {response.Message} ({response.Code}).");
            }

            var data = response.Data!;
            var receivedQuality = data.Quality;
            var maxAcceptable = data.AcceptQuality.First(q => q <= maxQuality);
            if (receivedQuality != maxAcceptable) {
                quality = maxAcceptable;
                throw new Exception(
                    $"Quality mismatch: received quality {receivedQuality}, best acceptable quality {quality}.");
            }

            if (receivedQuality > maxQuality) {
                throw new Exception(
                    $"Received quality ({receivedQuality}) more than requested {maxQuality}.");
            }

            var videos = data.Dash.Video.Where(v => v.Id == receivedQuality).ToList();
            var desiredCodecs = preferredCodec != null
                ? DesiredCodecs.Prepend(GetCodecId(preferredCodec)).ToList()
                : DesiredCodecs;
            var codec = desiredCodecs.FirstOrDefault(c => videos.Any(v => v.Codecid == c));
            if (codec == 0) {
                throw new Exception(
                    $"No desired code found: expected {string.Join(", ", desiredCodecs)}, found {string.Join(", ", videos.Select(v => v.Codecid))}");
            }

            var video = videos.First(v => v.Codecid == codec);
            var audio = data.Dash.Audio![0];
            var dolby = data.Dash.Dolby?.Audio?[0];
            var flac = data.Dash.Flac?.Audio;
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
                ((video.BackupUrl ?? Enumerable.Empty<string>()).Prepend(video.BaseUrl).Where(v => !IsIgnored(v)).ToList(),
                    videoSize),
                audios.Select(audio => (
                        (audio.BackupUrl ?? Enumerable.Empty<string>()).Prepend(audio.BaseUrl)
                        .Where(v => !IsIgnored(v)).ToList(),
                        audio.Bandwidth * data.Dash.Duration / 8))
                    .ToList());
        }, (ex, index) => {
            if (ex is BilibiliApiException || index > 5) {
                throw ex;
            }

            Logger.Warn(ex, $"Failed to get download links ({index}).");
            Thread.Sleep(TimeSpan.FromSeconds(10));
        });
    }

    static bool IsIgnored(string url) => IgnoredDomains.Any(d => url.Contains(d + "/"));

    static int GetCodecId(string preferredCodec)
        => CodecNames.First(c => c.Value == preferredCodec).Key;

    static string GetDownloadPage(string cid) {
        var response = HttpClients.BiliplusHttpClient
            .GetAsync($"https://www.biliplus.com/api/video_playurl?cid={cid}&type=mp4").Result;
        var content = response.GetString();
        Logger.Debug($"Downloaded page content: {content}");

        return content;
    }

    public static string? GetAid(string cid) {
        var response = HttpClients.BiliplusHttpClient
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
}
