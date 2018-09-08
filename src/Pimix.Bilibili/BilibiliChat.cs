using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using Newtonsoft.Json;

namespace Pimix.Bilibili {
    public class BilibiliChat {
        [JsonProperty("cid")]
        public string Cid { get; set; }

        XmlDocument rawDocument;

        public XmlDocument RawDocument {
            get {
                if (rawDocument == null) {
                    rawDocument = new XmlDocument();
                    using (var s = client.GetAsync($"http://comment.bilibili.com/{Cid}.xml")
                        .Result) {
                        rawDocument.Load(s.Content.ReadAsStreamAsync().Result);
                    }
                }

                return rawDocument;
            }
        }

        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        public TimeSpan ChatOffset { get; set; } = TimeSpan.Zero;

        readonly List<BilibiliComment> comments = new List<BilibiliComment>();

        public IEnumerable<BilibiliComment> Comments {
            get {
                if (comments.Count == 0) {
                    foreach (XmlNode comment in RawDocument.SelectNodes("//d")) {
                        comments.Add(new BilibiliComment(comment.Attributes["p"].Value,
                            comment.InnerText));
                    }
                }

                return comments.Select(c => c.WithOffset(ChatOffset));
            }
        }

        public string Title { get; set; } = "";

        readonly HttpClient client = new HttpClient(new HttpClientHandler {
            AutomaticDecompression = DecompressionMethods.Deflate
        });
    }
}
