using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace BiliBiliAssGenerator.Bilibili
{
    public class BilibiliVideo
    {
        static readonly Regex cidReg = new Regex(@"cid=\d+&");

        public string Aid { get; private set; }
        public string Title { get; set; }
        public List<BilibiliChat> Parts { get; set; }
        public string Description { get; private set; }
        public List<string> Keywords { get; private set; }

        public BilibiliVideo(string aid)
        {
            Aid = aid;
            HttpWebRequest request = WebRequest.CreateHttp("http://www.bilibili.com/video/av\{aid}");
            var document = new HtmlDocument();
            using (var stream = request.GetResponse().GetResponseStream())
            {
                document.Load(stream);
            }

            var dn = document.DocumentNode;
            Title = dn.SelectSingleNode("//meta[@name='title']/@content").InnerText;
            Description = dn.SelectSingleNode("//meta[@name='description']/@content").InnerText;
            Keywords = dn.SelectSingleNode("//meta[@name='keywords']/@content").InnerText.Split(',').ToList();
            var options = dn.SelectNodes("//option").Select(n => new Tuple<string, string>(n.Attributes["value"].Value, n.InnerText));
            if (options.Count() == 0)
            {
                // Single page
                Parts = new List<BilibiliChat>() { new BilibiliChat(FindCid(dn), "") };
            }
            else
            {
                // Multiple pages
                Parts = options.Select(option => new BilibiliChat(FindCid(dn), option.Item2)).ToList();
            }
        }

        string FindCid(HtmlNode documentNode)
            => cidReg.Match(documentNode.SelectNodes("//script")
                .First(s => s.InnerText.StartsWith("EmbedPlayer"))
                .InnerText).Groups[0].Value;
    }
}
