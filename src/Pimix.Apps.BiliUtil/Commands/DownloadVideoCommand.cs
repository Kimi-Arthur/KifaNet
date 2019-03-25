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

        [Value(1, Required = true, HelpText = "Download file to this folder.")]
        public string DownloadFolder { get; set; }

        [Option('i', "interactive", HelpText = "Choose source interactively.")]
        public bool Interactive { get; set; }

        [Option('s', "source", HelpText = "Override default source choice.")]
        public int SourceChoice { get; set; } = DefaultChoice;

        static readonly HttpClient biliplusClient = new HttpClient();

        public override int Execute() {
            biliplusClient.DefaultRequestHeaders.Add("cookie", Cookies);

            var segments = Aid.Split('p');
            var aid = segments.First();
            var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 1;
            var video = PimixService.Get<BilibiliVideo>(aid);

            var cid = video.Pages[pid - 1].Cid;
            var doc = new HtmlDocument();
            doc.LoadHtml(GetDownloadPage(cid));

            var choices = doc.DocumentNode.SelectNodes("//a").Select(linkNode
                => (name: linkNode.InnerText, link: linkNode.Attributes["href"].Value)).ToList();

            if (Interactive) {
                for (int i = 0; i < choices.Count; i++) {
                    Console.WriteLine($"[{i}] {choices[i].name}: {choices[i].link}");
                }

                Console.WriteLine($"Choose any of the sources above [0-{choices.Count - 1}]?");
                SourceChoice = int.Parse(Console.ReadLine());
            }

            var fileName = Confirm("Confirming download file name: ",
                Helper.GetDesiredFileName(aid, pid, cid));

            DownloadVideo(choices[SourceChoice].link,
                new PimixFile(DownloadFolder).GetFile($"{fileName}.mp4"));

            return 0;
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

        static void DownloadVideo(string url, PimixFile target) {
            logger.Info($"Downloading {url} to {target}.");
            using (var stream = biliplusClient.GetStreamAsync(url).Result) {
                target.Write(stream);
            }
        }
    }
}
