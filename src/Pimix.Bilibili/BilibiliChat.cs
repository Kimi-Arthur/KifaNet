using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;

namespace Pimix.Bilibili {
    public class BilibiliChat {
        readonly List<BilibiliComment> comments;

        public string Cid { get; }

        public TimeSpan ChatLength { get; set; }

        public TimeSpan ChatOffset { get; set; } = TimeSpan.Zero;

        public IEnumerable<BilibiliComment> Comments
            => comments.Select(c => c.WithOffset(ChatOffset));

        public string Title { get; set; }

        public BilibiliChat(string cid, string title) {
            Cid = cid;
            Title = title;
            var request = WebRequest.CreateHttp($"http://comment.bilibili.com/{cid}.xml");
            request.AutomaticDecompression = DecompressionMethods.Deflate;
            var document = new XmlDocument();
            using (var s = request.GetResponse().GetResponseStream()) {
                document.Load(s);
            }

            comments = new List<BilibiliComment>();
            foreach (XmlNode comment in document.SelectNodes("//d")) {
                comments.Add(new BilibiliComment(comment.Attributes["p"].Value, comment.InnerText));
            }
        }
    }
}
