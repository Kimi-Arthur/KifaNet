using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CommandLine;
using HtmlAgilityPack;
using NLog;
using Pimix.Api.Files;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("video", HelpText = "Download high quality Bilibili videos from biliplus.")]
    public class DownloadVideoCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string Cookies { get; set; }

        public static int DefaultChoice { get; set; }

        [Value(0, Required = true,
            HelpText = "The video id from Bilibili. With possible p{n} as a suffix.")]
        public string Aid { get; set; }

        [Option('i', "interactive", HelpText = "Choose source interactively.")]
        public bool Interactive { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = DefaultChoice;

        static HttpClient biliplusClient = new HttpClient();

        public override int Execute() {
            biliplusClient.DefaultRequestHeaders.Add("cookie", Cookies);

            var segments = Aid.Split('p');
            var aid = segments.First();
            var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 1;
            var video = PimixService.Get<BilibiliVideo>(aid);

            var cid = video.Pages[pid - 1].Cid;
            var doc = new HtmlDocument();
            doc.LoadHtml(GetDownloadPage(cid));

            var choices = new List<(string name, string link)>();
            foreach (var linkNode in doc.DocumentNode.SelectNodes("//a")) {
                choices.Add((linkNode.InnerText, linkNode.Attributes["href"].Value));
            }

            if (Interactive) {
                for (int i = 0; i < choices.Count; i++) {
                    Console.WriteLine($"[{i}] {choices[i].name}: {choices[i].link}");
                }

                Console.WriteLine($"Choose any of the sources above [0-{choices.Count - 1}]?");
                SourceChoice = int.Parse(Console.ReadLine());
            }

            DownloadVideo(choices[SourceChoice].link);

            return 0;
        }

        static string GetDownloadPage(string cid) {
            using (var response = biliplusClient
                .GetAsync($"https://www.biliplus.com/api/video_playurl?cid={cid}&type=mp4")
                .Result) {
                return response.GetString();
            }
        }

        static void DownloadVideo(string url) {
            logger.Info($"Downloading {url} to test.");
            var targetFile = new PimixFile("local:mac/Downloads/a.mp4");
            using (var stream = biliplusClient.GetStreamAsync(url).Result) {
                targetFile.Write(stream);
            }
        }
    }
}
