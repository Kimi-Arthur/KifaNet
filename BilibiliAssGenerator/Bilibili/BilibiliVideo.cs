using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BilibiliAssGenerator.Ass;
using HtmlAgilityPack;

namespace BilibiliAssGenerator.Bilibili
{
    public class BilibiliVideo
    {
        public enum PartModeType
        {
            SinglePartMode,
            ContinuousPartMode,
            ParallelPartMode
        }

        static readonly Regex cidReg = new Regex(@"cid=(\d+)&");

        public string Aid { get; private set; }

        public string Title { get; set; }

        public PartModeType PartMode { get; set; }

        public IEnumerable<BilibiliChat> Parts { get; set; }

        public string Description { get; set; }

        public IEnumerable<string> Keywords { get; set; }

        public BilibiliVideo(string aid)
        {
            Aid = aid;
            HttpWebRequest request = WebRequest.CreateHttp($"http://www.bilibili.com/video/av{aid}");
            request.AutomaticDecompression = DecompressionMethods.GZip;
            AddCookies(request);

            var document = new HtmlDocument();
            using (var stream = request.GetResponse().GetResponseStream())
            {
                document.Load(stream, Encoding.UTF8);
            }

            var documentNode = document.DocumentNode;
            Title = documentNode.SelectSingleNode("//meta[@name='title']").Attributes["content"].Value;
            Description = documentNode.SelectSingleNode("//meta[@name='description']").Attributes["content"].Value;
            Keywords = documentNode.SelectSingleNode("//meta[@name='keywords']").Attributes["content"].Value.Split(',').ToList();
            var options = documentNode.SelectNodes("//option")?.Select(n => n.Attributes["value"].Value);

            if (options == null)
            {
                // Single page
                Parts = new List<BilibiliChat>() { new BilibiliChat(FindCid(documentNode), "") };
                PartMode = PartModeType.SinglePartMode;
            }
            else
            {
                // Multiple pages
                var titles = documentNode.SelectSingleNode("//select").InnerText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var parts = new List<BilibiliChat>() { new BilibiliChat(FindCid(documentNode), titles[0]) };
                PartMode = PartModeType.ContinuousPartMode;
                foreach (var option in options.Skip(1).Zip(titles.Skip(1), (x, y) => Tuple.Create(x, y)))
                {
                    HttpWebRequest subpageRequest = WebRequest.CreateHttp($"http://www.bilibili.com{option.Item1}");
                    subpageRequest.AutomaticDecompression = DecompressionMethods.GZip;
                    AddCookies(subpageRequest);

                    var subpageDocument = new HtmlDocument();
                    using (var stream = subpageRequest.GetResponse().GetResponseStream())
                    {
                        subpageDocument.Load(stream, Encoding.UTF8);
                    }

                    var subpageDocumentNode = subpageDocument.DocumentNode;
                    parts.Add(new BilibiliChat(FindCid(subpageDocumentNode), option.Item2));
                }

                Parts = parts;
            }
        }

        public AssDocument GenerateAssDocument()
        {
            AssDocument result = new AssDocument();
            result.Sections.Add(new AssScriptInfoSection() { Title = Title, OriginalScript = "Bilibili" });
            TimeSpan timeOffset = TimeSpan.Zero;

            foreach (var part in Parts)
            {
                part.ChatOffset = timeOffset;
                foreach (var comment in part.Comments)
                {
                    comment.GenerateAssDialogue();
                }

                if (PartMode == PartModeType.ContinuousPartMode)
                {
                    timeOffset = timeOffset.Add(part.ChatLength);
                }
            }

            return result;
        }

        void AddCookies(HttpWebRequest request)
        {
            string cookies = "pgv_pvi=7139182592; sid=lwi4gq8b; tma=136533283.66239416.1418041709236.1418041709236.1418041709236.1; tmd=4.136533283.66239416.1418041709236.; DedeUserID=3888766; DedeUserID__ckMd5=7476605d2f1afaa1; SESSDATA=60e8e8eb%2C1423190202%2Cc0a30aff; sid=8464zjec; pgv_si=s346497024; _cnt_dyn=0; _cnt_pm=0; _cnt_notify=0; __track_isp_speed=1; CNZZDATA2724999=cnzz_eid%3D736815101-1418041708-%26ntime%3D1420973401";
            request.CookieContainer = new CookieContainer();
            foreach (var cookie in cookies.Split(';'))
            {
                var results = cookie.Split('=').Select(x => x.Trim()).ToList();
                request.CookieContainer.Add(new Cookie(results[0], results[1], "/", ".bilibili.com"));
            }
        }

        string FindCid(HtmlNode documentNode)
            => cidReg.Match(documentNode.SelectNodes("//script")
                .First(s => s.InnerText.StartsWith("EmbedPlayer"))
                .InnerText).Groups[1].Value;
    }
}
