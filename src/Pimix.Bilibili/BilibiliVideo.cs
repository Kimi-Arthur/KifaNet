using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Pimix.Ass;
using Pimix.Service;

namespace Pimix.Bilibili {
    [DataModel("bilibili/videos")]
    public class BilibiliVideo {
        public enum PartModeType {
            SinglePartMode,
            ContinuousPartMode,
            ParallelPartMode
        }

        static readonly Regex cidReg = new Regex(@"videoshot/(\d+)-");

        [JsonProperty("id")]
        public string Id { get; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("uploaded")]
        public DateTime Uploaded { get; set; }

        [JsonProperty("pages")]
        public IEnumerable<BilibiliChat> Pages { get; set; }

        PartModeType partMode;

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

        public BilibiliVideo(string id) {
            Id = id;
            var request =
                WebRequest.CreateHttp($"http://www.bilibili.com/video/av{id}");
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
            Tags = documentNode.SelectSingleNode("//meta[@name='keywords']")
                .Attributes["content"].Value.Split(',').ToList();
            var options = documentNode.SelectNodes("//option")
                ?.Select(n => n.Attributes["value"].Value);

            if (options == null) {
                // Single page
                Pages = new List<BilibiliChat> {
                    new BilibiliChat {
                        Cid = FindCid(documentNode)
                    }
                };
                PartMode = PartModeType.SinglePartMode;
            } else {
                // Multiple pages
                var titles = documentNode.SelectSingleNode("//select").InnerText
                    .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();
                var parts = new List<BilibiliChat> {
                    new BilibiliChat {
                        Cid = FindCid(documentNode),
                        Title = titles[0]
                    }
                };
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
                    parts.Add(new BilibiliChat
                        {Cid = FindCid(subpageDocumentNode), Title = option.Item2});
                }

                Pages = parts;
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

            foreach (var part in Pages)
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
