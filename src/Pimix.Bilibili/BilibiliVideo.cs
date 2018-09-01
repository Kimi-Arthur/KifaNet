using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Pimix.Ass;

namespace Pimix.Bilibili {
    public class BilibiliVideo {
        public enum PartModeType {
            SinglePartMode,
            ContinuousPartMode,
            ParallelPartMode
        }

        static readonly Regex cidReg = new Regex(@"videoshot/(\d+)-");

        public string Aid { get; }

        public string Title { get; set; }

        PartModeType partMode;

        public PartModeType PartMode {
            get => partMode;
            set {
                partMode = value;
                if (partMode == PartModeType.ContinuousPartMode) {
                    var offset = TimeSpan.Zero;
                    foreach (var part in Parts) {
                        part.ChatOffset = offset;
                        offset += part.ChatLength;
                    }
                } else {
                    foreach (var part in Parts) {
                        part.ChatOffset = TimeSpan.Zero;
                    }
                }
            }
        }

        public IEnumerable<BilibiliChat> Parts { get; set; }

        public string Description { get; set; }

        public IEnumerable<string> Keywords { get; set; }

        public BilibiliVideo(string aid) {
            Aid = aid;
            var request =
                WebRequest.CreateHttp($"http://www.bilibili.com/video/av{aid}");
            request.AutomaticDecompression = DecompressionMethods.GZip;
            AddCookies(request);

            var document = new HtmlDocument();
            using (var stream = request.GetResponse().GetResponseStream()) {
                document.Load(stream, Encoding.UTF8);
            }

            var documentNode = document.DocumentNode;
            Title = documentNode.SelectSingleNode("//meta[@name='title']").Attributes["content"]
                .Value;
            Description = documentNode.SelectSingleNode("//meta[@name='description']")
                .Attributes["content"].Value;
            Keywords = documentNode.SelectSingleNode("//meta[@name='keywords']")
                .Attributes["content"].Value.Split(',').ToList();
            var options = documentNode.SelectNodes("//option")
                ?.Select(n => n.Attributes["value"].Value);

            if (options == null) {
                // Single page
                Parts = new List<BilibiliChat> {new BilibiliChat(FindCid(documentNode), "")};
                PartMode = PartModeType.SinglePartMode;
            } else {
                // Multiple pages
                var titles = documentNode.SelectSingleNode("//select").InnerText
                    .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();
                var parts = new List<BilibiliChat>
                    {new BilibiliChat(FindCid(documentNode), titles[0])};
                foreach (var option in options.Skip(1)
                    .Zip(titles.Skip(1), (x, y) => Tuple.Create(x, y))) {
                    var subpageRequest =
                        WebRequest.CreateHttp($"http://www.bilibili.com{option.Item1}");
                    subpageRequest.AutomaticDecompression = DecompressionMethods.GZip;
                    AddCookies(subpageRequest);

                    var subpageDocument = new HtmlDocument();
                    using (var stream = subpageRequest.GetResponse().GetResponseStream()) {
                        subpageDocument.Load(stream, Encoding.UTF8);
                    }

                    var subpageDocumentNode = subpageDocument.DocumentNode;
                    parts.Add(new BilibiliChat(FindCid(subpageDocumentNode), option.Item2));
                }

                Parts = parts;
                PartMode = PartModeType.ContinuousPartMode;
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

            foreach (var part in Parts)
            foreach (var comment in part.Comments) {
                events.Events.Add(comment.GenerateAssDialogue());
            }

            return result;
        }

        void AddCookies(HttpWebRequest request) {
            var cookies =
                "buvid3=5F6F28B3-78AD-4032-AEC9-9B23C523E56116637infoc";
            request.CookieContainer = new CookieContainer();
            foreach (var cookie in cookies.Split(';')) {
                var results = cookie.Split('=').Select(x => x.Trim()).ToList();
                request.CookieContainer.Add(
                    new Cookie(results[0], results[1], "/", ".bilibili.com"));
            }
        }

        string FindCid(HtmlNode documentNode)
            => cidReg.Match(documentNode
                .SelectNodes("//div[@class='bilibili-player-video-progress-detail-img']")
                .First().GetAttributeValue("style", "")).Groups[1].Value;
    }
}
