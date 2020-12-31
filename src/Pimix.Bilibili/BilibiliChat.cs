using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace Pimix.Bilibili {
    public class BilibiliChat {
        readonly HttpClient client = new HttpClient(new HttpClientHandler {
            AutomaticDecompression = DecompressionMethods.Deflate
        });

        readonly List<BilibiliComment> comments = new List<BilibiliComment>();

        XmlDocument rawDocument;

        public int Id { get; set; }

        public string Cid { get; set; }

        public string Title { get; set; } = "";

        public TimeSpan Duration { get; set; }

        [JsonIgnore]
        public XmlDocument RawDocument {
            get {
                if (rawDocument == null) {
                    using var s = client.GetAsync($"http://comment.bilibili.com/{Cid}.xml")
                        .Result;
                    var content = string.Concat(s.Content.ReadAsStringAsync().Result.Where(XmlConvert.IsXmlChar));
                    Load(new MemoryStream(Encoding.UTF8.GetBytes(content)));
                }

                return rawDocument;
            }
        }

        [JsonIgnore]
        public TimeSpan ChatOffset { get; set; } = TimeSpan.Zero;

        [JsonIgnore]
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

        public void Load(Stream stream) {
            rawDocument = new XmlDocument();
            rawDocument.Load(stream);
            stream.Dispose();
        }
    }
}
