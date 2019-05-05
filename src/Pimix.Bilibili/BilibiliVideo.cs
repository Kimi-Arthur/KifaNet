using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Pimix.Service;
using Pimix.Subtitle.Ass;

namespace Pimix.Bilibili {
    [DataModel("bilibili/videos")]
    public class BilibiliVideo {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly HttpClient biliplusClient = new HttpClient();

        public static string BiliplusCookies { get; set; }
        public static int DefaultBiliplusSourceChoice { get; set; }

        public enum PartModeType {
            SinglePartMode,
            ContinuousPartMode,
            ParallelPartMode
        }

        public string Id { get; set; }
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

        PartModeType partMode;

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
            result.Sections.Add(new AssScriptInfoSection
                {Title = Title, OriginalScript = "Bilibili"});
            result.Sections.Add(new AssStylesSection
                {Styles = new List<AssStyle> {AssStyle.DefaultStyle}});
            var events = new AssEventsSection();
            result.Sections.Add(events);

            foreach (var part in Pages)
            foreach (var comment in part.Comments) {
                events.Events.Add(comment.GenerateAssDialogue());
            }

            return result;
        }

        public string GetDesiredName(int pid, string cid = null) {
            var p = Pages.First(x => x.Id == pid);

            if (cid != null && cid != p.Cid) {
                return null;
            }

            return Pages.Count > 1
                ? $"{$"{Author}-{AuthorId}".NormalizeFileName()}" +
                  $"/{$"{Title} P{pid} {p.Title}".NormalizeFileName()}-{Id}p{pid}.c{p.Cid}"
                : $"{$"{Author}-{AuthorId}".NormalizeFileName()}" +
                  $"/{$"{Title} {p.Title}".NormalizeFileName()}-{Id}.c{p.Cid}";
        }

        public Stream DownloadVideo(int pid, int biliplusSourceChoice = 0) {
            biliplusClient.DefaultRequestHeaders.Add("cookie", BiliplusCookies);

            var added = AddDownloadJob(Id, pid);

            var cid = Pages[pid - 1].Cid;
            var doc = new HtmlDocument();
            doc.LoadHtml(GetDownloadPage(cid));

            var choices = doc.DocumentNode.SelectNodes("//a")?.Select(linkNode
                => (name: linkNode.InnerText, link: linkNode.Attributes["href"].Value)).ToList();

            while (added && choices == null) {
                doc = new HtmlDocument();
                doc.LoadHtml(GetDownloadPage(cid));

                choices = doc.DocumentNode.SelectNodes("//a")?.Select(linkNode
                        => (name: linkNode.InnerText, link: linkNode.Attributes["href"].Value))
                    .ToList();

                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            if (choices == null) {
                logger.Warn("No sources found. Job not successful?");
                return null;
            }

            var initialSource = biliplusSourceChoice;
            while (true) {
                try {
                    logger.Debug(
                        $"Choosen source: " +
                        $"{choices[biliplusSourceChoice].name}({choices[biliplusSourceChoice].link})");
                    return new HttpClient().GetStreamAsync(choices[biliplusSourceChoice].link).Result;
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

        static bool AddDownloadJob(string aid, int pid) {
            using (var response = biliplusClient
                .GetAsync(
                    $"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&page={pid}")
                .Result) {
                var content = response.GetString();
                logger.Debug($"Add download request result: {content}");
                var code = (int) JToken.Parse(content)["code"];
                return code == 0;
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
    }
}
