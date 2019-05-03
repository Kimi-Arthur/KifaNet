using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using CommandLine;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
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

        static readonly HttpClient biliplusClient = new HttpClient();

        public override int Execute() {
            biliplusClient.DefaultRequestHeaders.Add("cookie", Cookies);

            var segments = Aid.Split('p');
            var aid = segments.First();
            var pid = segments.Length == 2 ? int.Parse(segments.Last()) : 1;

            var added = AddDownloadJob(aid, pid);

            PimixService.Update(new BilibiliVideo {Id = aid});
            var video = PimixService.Get<BilibiliVideo>(aid);

            var cid = video.Pages[pid - 1].Cid;
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
                Console.WriteLine("No sources found. Job not successful?");
                return 1;
            }

            if (Interactive) {
                for (int i = 0; i < choices.Count; i++) {
                    Console.WriteLine($"[{i}] {choices[i].name}: {choices[i].link}");
                }

                Console.WriteLine($"Choose any of the sources above [0-{choices.Count - 1}]?");
                SourceChoice = int.Parse(Console.ReadLine());
            }

            DownloadVideo(choices[SourceChoice].link,
                CurrentFolder.GetFile($"{Helper.GetDesiredFileName(video, pid, cid)}.mp4"));

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

        static bool AddDownloadJob(string aid, int pid) {
            using (var response = biliplusClient
                .GetAsync(
                    $"https://www.biliplus.com/api/saver_add?aid={aid.Substring(2)}&page={pid}")
                .Result) {
                var content = response.GetString();
                var code = (int) JToken.Parse(content)["code"];
                logger.Debug($"Add download request result: {content}");
                return code == 0;
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
