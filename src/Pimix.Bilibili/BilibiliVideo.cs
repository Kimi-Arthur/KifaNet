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
using Pimix.IO;
using Pimix.Service;
using Pimix.Subtitle.Ass;

namespace Pimix.Bilibili {
    public class BilibiliVideo : DataModel {
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

        public const string ModelId = "bilibili/videos";

        static PimixServiceClient<BilibiliVideo> client;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static bool firstDownload = true;

        static HttpClient biliplusClient = new HttpClient();

        PartModeType partMode;

        public static PimixServiceClient<BilibiliVideo> Client
            => client =
                client ?? new PimixServiceRestClient<BilibiliVideo>();

        public static string BiliplusCookies { get; set; }
        public static int DefaultBiliplusSourceChoice { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }
        public string AuthorId { get; set; }
        public string Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Tags { get; set; }
        public string Category { get; set; }
        public string Cover { get; set; }
        public DateTime? Uploaded { get; set; }
        public List<BilibiliChat> Pages { get; set; }

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

        public string GetDesiredName(int pid, string cid = null, string extraPath = null) {
            var p = Pages.First(x => x.Id == pid);

            if (cid != null && cid != p.Cid) {
                return null;
            }

            return $"{$"{Author}-{AuthorId}".NormalizeFileName()}" +
                   (extraPath == null ? "" : $"/{extraPath}") +
                   (Pages.Count > 1
                       ? $"/{$"{Title} P{pid} {p.Title}".NormalizeFileName()}-{Id}p{pid}.c{p.Cid}"
                       : $"/{$"{Title} {p.Title}".NormalizeFileName()}-{Id}.c{p.Cid}");
        }

        public Stream DownloadVideo(int pid, int biliplusSourceChoice = 0) {
            if (!firstDownload) {
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            firstDownload = false;

            biliplusClient = new HttpClient();
            biliplusClient.DefaultRequestHeaders.Add("cookie", BiliplusCookies);

            AddDownloadJob(Id);

            var cid = Pages[pid - 1].Cid;

            while (GetDownloadStatus(Id, pid) == DownloadStatus.InProgress) {
                logger.Debug("Download not ready. Sleep 30 seconds...");
                UpdateDownloadStatus(cid);
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(GetDownloadPage(cid));

            var choices = doc.DocumentNode.SelectNodes("//a")?.Select(linkNode
                => (name: linkNode.InnerText, link: linkNode.Attributes["href"].Value)).ToList();

            if (choices == null) {
                logger.Warn("No sources found. Job not successful?");
                return null;
            }

            var initialSource = biliplusSourceChoice;
            while (true) {
                try {
                    logger.Debug("Choosen source: " +
                                 $"{choices[biliplusSourceChoice].name}({choices[biliplusSourceChoice].link})");
                    var length = biliplusClient
                        .SendAsync(
                            new HttpRequestMessage(HttpMethod.Head,
                                choices[biliplusSourceChoice].link),
                            HttpCompletionOption.ResponseHeadersRead).Result
                        .Content.Headers.ContentLength;
                    var link = choices[biliplusSourceChoice].link;
                    if (length == null) {
                        throw new Exception("Content length is not found.");
                    }

                    return new SeekableReadStream(length.Value,
                        (buffer, bufferOffset, offset, count) => {
                            if (count < 0) {
                                count = buffer.Length - bufferOffset;
                            }

                            logger.Trace(
                                $"Downloading from {offset} to {offset + count}...");

                            return Retry.Run(() => {
                                var request = new HttpRequestMessage(HttpMethod.Get, link);

                                request.Headers.Range =
                                    new RangeHeaderValue(offset, offset + count - 1);
                                using (var response = biliplusClient.SendAsync(request).Result) {
                                    var memoryStream =
                                        new MemoryStream(buffer, bufferOffset, count, true);
                                    response.Content.ReadAsStreamAsync().Result
                                        .CopyTo(memoryStream, count);
                                    return (int) memoryStream.Position;
                                }
                            }, (ex, i) => {
                                if (i >= 5) {
                                    throw ex;
                                }

                                logger.Warn(ex,
                                    $"Download from {offset} to {offset + count} failed ({i})...");
                                Thread.Sleep(TimeSpan.FromSeconds(30));
                            });
                        });
                } catch (Exception ex) {
                    biliplusSourceChoice = (biliplusSourceChoice + 1) % choices.Count;
                    if (biliplusSourceChoice == initialSource) {
                        throw;
                    }

                    logger.Warn(ex, "Download failed. Try next source.");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
            }
        }

        static void AddDownloadJob(string aid) {
            using (var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&checkall")
                .Result) {
                var content = response.GetString();
                logger.Debug($"Add download request result: {content}");
            }
        }

        static void UpdateDownloadStatus(string cid) {
            using (var response = biliplusClient
                .GetAsync($"https://bg.biliplus-vid.top/api/saver_status.php?cid={cid}")
                .Result) {
                var content = response.GetString();
                logger.Debug($"Check saver status: {content}");
            }
        }

        static DownloadStatus GetDownloadStatus(string aid, int pid) {
            using (var response = biliplusClient
                .GetAsync(
                    $"https://www.biliplus.com/api/geturl?bangumi=0&av={aid.Substring(2)}&page={pid}")
                .Result) {
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
        }

        static string GetDownloadPage(string cid) {
            using (var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/video_playurl?cid={cid}&type=mp4")
                .Result) {
                var content = response.GetString();
                logger.Debug($"Downloaded page content: {content}");

                return content;
            }
        }

        public static string GetAid(string cid) {
            using (var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/cidinfo?cid={cid}")
                .Result) {
                var content = response.GetString();
                logger.Debug($"Cid info: {content}");

                var data = JToken.Parse(content)["data"];

                return $"av{data["aid"]}p{data["page"]}";
            }
        }
    }
}
