using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Bilibili;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("comments", HelpText = "Get comments for the video from Bilibili.")]
    class GetCommentsCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to get comments for.")]
        public string FileUri { get; set; }

        [Option('c', "cid", HelpText = "Bilibili cid for comments.")]
        public string Cid { get; set; }

        [Option('a', "aid", HelpText =
            "Bilibili aid for the video. It can contain one segment or multiple." +
            "Example: av2044037, av2044037p4")]
        public string Aid { get; set; }

        [Option('g', "group", HelpText = "Group name.")]
        public string Group { get; set; }

        public override int Execute() {
            if (Aid != null) {
                var files = new PimixFile(FileUri).List(true).ToList();

                var ids = Aid.Split('p');
                var v = BilibiliVideo.Get(ids[0]);
                foreach (var item in v.Pages.Zip(files, Tuple.Create)) {
                    Console.WriteLine(
                        $"{v.Title} - {item.Item1.Title}\n" +
                        $"{item.Item2}\n" +
                        $"{v.Id}p{item.Item1.Id} (cid={item.Item1.Cid})\n");
                }

                Console.Write(
                    $"Confirm getting the {Math.Min(v.Pages.Count, files.Count)} Bilibili chats above?");
                Console.ReadLine();

                return v.Pages.Zip(files, GetChat).Max();
            }

            return GetChat(new BilibiliChat {Cid = Cid}, new PimixFile(FileUri));
        }

        int GetChat(BilibiliChat chat, PimixFile rawFile) {
            var memoryStream = new MemoryStream();
            var writer = new XmlTextWriter(memoryStream, new UpperCaseUtf8Encoding()) {
                Formatting = Formatting.Indented
            };
            chat.RawDocument.Save(writer);

            memoryStream.Seek(0, SeekOrigin.Begin);

            var suffix = Group != null ? $"-{Group}" : "";
            var lastDot = rawFile.ToString().LastIndexOf(".", StringComparison.Ordinal);
            var targetUri = $"{rawFile.ToString().Substring(0, lastDot)}.{chat.Cid}{suffix}.xml";
            var target = new PimixFile(targetUri);
            target.Write(memoryStream);

            memoryStream.Dispose();

            return 0;
        }
    }

    class UpperCaseUtf8Encoding : UTF8Encoding {
        public override string WebName => base.WebName.ToUpper();
    }
}
