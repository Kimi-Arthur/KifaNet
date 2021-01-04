using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Pimix.Bilibili.BilibiliApi;
using Pimix.Bilibili.BiliplusApi;
using Pimix.IO;
using Pimix.Service;
using Pimix.Subtitle.Ass;

namespace Pimix.Bilibili {
    public class BilibiliVideoStats {
        public long PlayCount { get; set; }
        public long DanmakuCount { get; set; }
        public long CoinCount { get; set; }
        public long LikeCount { get; set; }
        public long FavoriteCount { get; set; }
        public long ReplyCount { get; set; }
        public long ShareCount { get; set; }
    }

    public class BilibiliVideo : DataModel {
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

        static PimixServiceClient<BilibiliVideo> client;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static bool firstDownload = true;

        static HttpClient bilibiliClient = GetBilibiliClient();

        static HttpClient biliplusClient = GetBiliplusClient();

        PartModeType partMode;

        public static PimixServiceClient<BilibiliVideo> Client =>
            client ??= new PimixServiceRestClient<BilibiliVideo>();

        public static string BilibiliCookies { get; set; }

        public static string BiliplusCookies { get; set; }
        public static int DefaultBiliplusSourceChoice { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }
        public string AuthorId { get; set; }
        public string Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Category { get; set; }
        public Uri Cover { get; set; }
        public DateTimeOffset? Uploaded { get; set; }
        public List<BilibiliChat> Pages { get; set; }
        public BilibiliVideoStats Stats { get; set; } = new BilibiliVideoStats();

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

        public override void Fill() {
            try {
                if (FillWithBilibili()) {
                    return;
                }
            } catch (Exception e) {
                logger.Debug(e, $"Unable to find video {Id} from bilibili API.");
            }

            try {
                if (FillWithBiliplus()) {
                    return;
                }
            } catch (Exception e) {
                logger.Debug(e, $"Unable to find video {Id} from biliplus API.");
            }

            try {
                FillWithBiliplusCache();
            } catch (Exception e) {
                logger.Debug(e, $"Unable to find video {Id} from biliplus cache.");
            }
        }

        bool FillWithBilibili() {
            var data = new VideoRpc().Call(Id).Data;
            Title = data.Title;
            Author = data.Owner.Name;
            AuthorId = data.Owner.Mid.ToString();
            Description = data.Desc;
            // TODO: tagname from https://api.bilibili.com/x/tag/archive/tags?aid=170001
            Category = data.Tname;
            Cover = data.Pic;
            Pages = data.Pages.Select(p => new BilibiliChat {Id = p.Page, Cid = p.Cid.ToString(), Title = p.Part})
                .ToList();
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
            var data = new BiliplusVideoRpc().Call(Id);
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
                    Id = p.Page, Cid = p.Cid.ToString(), Title = p.Part, Duration = TimeSpan.FromSeconds(p.Duration)
                }).ToList();
                Uploaded = DateTimeOffset.FromUnixTimeSeconds(v2.Pubdate);
                Uploaded = Uploaded.Value.ToOffset(TimeZones.ShanghaiTimeZone.GetUtcOffset(Uploaded.Value));

                var stat = v2.Stat;
                Stats.PlayCount = stat.View;
                Stats.DanmakuCount = stat.Danmaku;
                Stats.CoinCount = stat.Coin;
                Stats.LikeCount = stat.Like;
                Stats.FavoriteCount = stat.Favorite;
                Stats.ReplyCount = stat.Reply;
                Stats.ShareCount = stat.Share;
            } else {
                Title = data.Title;
                Author = data.Author;
                AuthorId = data.Mid.ToString();
                Description = data.Description;
                Tags = data.Tag.Split(",").ToList();
                Category = data.Typename;
                Cover = data.Pic;
                Pages = data.List.Select(p => new BilibiliChat {Id = p.Page, Cid = p.Cid.ToString(), Title = p.Part})
                    .ToList();
                Uploaded = DateTimeOffset.FromUnixTimeSeconds(v2.Pubdate);
                Uploaded = Uploaded.Value.ToOffset(TimeZones.ShanghaiTimeZone.GetUtcOffset(Uploaded.Value));

                Stats.PlayCount = data.Play;
                Stats.DanmakuCount = data.VideoReview;
                Stats.CoinCount = data.Coins;
                Stats.FavoriteCount = data.Favorites;
                Stats.ReplyCount = data.Review;
            }

            return true;
        }

        bool FillWithBiliplusCache() {
            var data = new BiliplusVideoCacheRpc().Call(Id).Data;
            var info = data.Info;
            Title = info.Title;
            Author = info.Author;
            AuthorId = info.Mid.ToString();
            Description = info.Description;
            Tags = info.Keywords.Split(",").ToList();
            Category = info.Typename;
            Cover = info.Pic;
            Pages = data.Parts.Select(p => new BilibiliChat {Id = p.Page, Cid = p.Cid.ToString(), Title = p.Part})
                .ToList();
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
            result.Sections.Add(new AssScriptInfoSection {Title = Title, OriginalScript = "Bilibili"});
            result.Sections.Add(new AssStylesSection {Styles = new List<AssStyle> {AssStyle.DefaultStyle}});
            var events = new AssEventsSection();
            result.Sections.Add(events);

            foreach (var part in Pages)
            foreach (var comment in part.Comments) {
                events.Events.Add(comment.GenerateAssDialogue());
            }

            return result;
        }

        public string GetDesiredName(int pid, string cid = null, string extraPath = null, bool prefixDate = false) {
            var p = Pages.First(x => x.Id == pid);

            if (cid != null && cid != p.Cid) {
                return null;
            }

            var partName = p.Title.NormalizeFileName();
            var title = Title.NormalizeFileName();
            if (title.StartsWith(partName)) {
                partName = "";
            } else if (partName.StartsWith(title)) {
                partName = partName.Substring(title.Length);
            }

            var prefix = prefixDate ? $"{Uploaded.Value:yyyy-MM-dd}" : "";
            var pidText = $"P{pid.ToString("D" + Pages.Count.ToString().Length)}";

            return $"{$"{Author}-{AuthorId}".NormalizeFileName()}" + (extraPath == null ? "" : $"/{extraPath}") +
                   (Pages.Count > 1
                       ? $"/{$"{prefix} {title} {pidText} {partName}".NormalizeFileName()}-{Id}p{pid}.c{p.Cid}"
                       : $"/{$"{prefix} {title} {partName}".NormalizeFileName()}-{Id}.c{p.Cid}");
        }

        public (string extension, List<Func<Stream>> streamGetters) GetVideoStreams(int pid,
            int biliplusSourceChoice = 0) {
            if (!firstDownload) {
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            firstDownload = false;

            var cid = Pages[pid - 1].Cid;

            if (UseMergedSource) {
                AddDownloadJob(Id);

                while (GetDownloadStatus(Id, pid) == DownloadStatus.InProgress) {
                    logger.Debug("Download not ready. Sleep 30 seconds...");
                    UpdateDownloadStatus(cid);
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(GetDownloadPage(cid));

                var choices = doc.DocumentNode.SelectNodes("//a")?.Select(linkNode =>
                    (name: linkNode.InnerText, link: linkNode.Attributes["href"].Value)).ToList();

                if (choices == null) {
                    logger.Warn("No sources found. Job not successful?");
                    return (null, null);
                }

                var initialSource = biliplusSourceChoice;
                while (true) {
                    try {
                        logger.Debug("Choosen source: " +
                                     $"{choices[biliplusSourceChoice].name}({choices[biliplusSourceChoice].link})");
                        var link = choices[biliplusSourceChoice].link;
                        return ("mp4", new List<Func<Stream>> {() => BuildDownloadStream(link)});
                    } catch (Exception ex) {
                        biliplusSourceChoice = (biliplusSourceChoice + 1) % choices.Count;
                        if (biliplusSourceChoice == initialSource) {
                            throw;
                        }

                        logger.Warn(ex, "Download failed. Try next source.");
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }
            } else {
                var (extension, links) = GetDownloadLinks(Id, cid);
                return extension == null
                    ? (null, null)
                    : (extension, links.Select<string, Func<Stream>>(l => () => BuildDownloadStream(l)).ToList());
            }
        }

        static Stream BuildDownloadStream(string link) {
            var length = bilibiliClient.GetContentLength(link);
            if (length == null) {
                throw new Exception("Content length is not found.");
            }

            return new SeekableReadStream(length.Value, (buffer, bufferOffset, offset, count) => {
                if (count < 0) {
                    count = buffer.Length - bufferOffset;
                }

                logger.Trace($"Downloading from {offset} to {offset + count} of {link}...");

                return Retry.Run(() => {
                    var request = new HttpRequestMessage(HttpMethod.Get, link);

                    request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
                    using var response = bilibiliClient.SendAsync(request).Result;
                    response.EnsureSuccessStatusCode();
                    var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                    response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
                    return (int) memoryStream.Position;
                }, (ex, i) => {
                    if (i >= 5) {
                        throw ex;
                    }

                    logger.Warn(ex, $"Download from {offset} to {offset + count} failed ({i})...");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                });
            });
        }

        static void AddDownloadJob(string aid) {
            using var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&checkall").Result;
            var content = response.GetString();
            logger.Debug($"Add download request result: {content}");
        }

        static void UpdateDownloadStatus(string cid) {
            using var response = biliplusClient.GetAsync($"https://bg.biliplus-vid.top/api/saver_status.php?cid={cid}")
                .Result;
            var content = response.GetString();
            logger.Debug($"Check saver status: {content}");
        }

        static DownloadStatus GetDownloadStatus(string aid, int pid) {
            using var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/geturl?bangumi=0&av={aid.Substring(2)}&page={pid}").Result;
            var content = response.GetString();
            logger.Debug($"Get download result: {content}");
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

        static (string extension, List<string> links) GetDownloadLinks(string aid, string cid) {
            var quality = 120;
            while (true) {
                using var response = bilibiliClient
                    .GetAsync(
                        $"https://api.bilibili.com/x/player/playurl?cid={cid}&avid={aid.Substring(2)}&qn={quality}&fourk=1")
                    .Result;
                var content = response.GetString();
                logger.Debug($"Get download result: {content}");
                var data = JToken.Parse(content);
                if ((int) data["code"] != 0) {
                    logger.Warn($"bilibili API error: {data["message"]} ({data["code"]}).");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    continue;
                }

                if ((int) data["data"]["quality"] != (int) data["data"]["accept_quality"][0]) {
                    quality = (int) data["data"]["accept_quality"][0];
                    logger.Warn($"Quality mismatch: received quality {data["data"]["quality"]}, " +
                                $"best quality {data["data"]["accept_quality"][0]}.");
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    continue;
                }

                var urls = data["data"]["durl"];
                var extension = (string) urls[0]["url"];
                extension = extension[..extension.IndexOf('?')];
                extension = extension[(extension.LastIndexOf('.') + 1)..];
                return (extension, urls.Select(x => (string) x["url"]).ToList());
            }
        }

        static string GetDownloadPage(string cid) {
            using var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/video_playurl?cid={cid}&type=mp4").Result;
            var content = response.GetString();
            logger.Debug($"Downloaded page content: {content}");

            return content;
        }

        public static string GetAid(string cid) {
            using var response = biliplusClient.GetAsync($"https://www.biliplus.com/api/cidinfo?cid={cid}").Result;
            var content = response.GetString();
            logger.Debug($"Cid info: {content}");

            var data = JToken.Parse(content)["data"];
            return $"av{data["aid"]}p{data["page"]}";
        }

        public static HttpClient GetBilibiliClient() {
            var client = new HttpClient {Timeout = TimeSpan.FromMinutes(10)};
            client.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
            return client;
        }

        public static HttpClient GetBiliplusClient() {
            var client = new HttpClient {Timeout = TimeSpan.FromMinutes(10)};
            client.DefaultRequestHeaders.Add("cookie", BiliplusCookies);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            return client;
        }
    }
}
