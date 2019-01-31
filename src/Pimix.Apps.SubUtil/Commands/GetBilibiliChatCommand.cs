using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("bilibili", HelpText = "Get Bilibili chat as xml document.")]
    class GetBilibiliChatCommand : SubUtilCommand {
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
                var v = PimixService.Get<BilibiliVideo>(ids[0]);
                foreach (var item in v.Pages.Zip(files, Tuple.Create)) {
                    Console.WriteLine($"{v.Title} - {item.Item1.Title}\n" +
                                      $"{item.Item2}\n" +
                                      $"{v.Id}p{item.Item1.Id} (cid={item.Item1.Cid})\n");
                }

                Console.Write($"Confirm getting the {Math.Min(v.Pages.Count, files.Count)} Bilibili chats above?");
                Console.ReadLine();

                return v.Pages.Zip(files, GetChat).Max();
            }

            if (Cid == null) {
                // Needs to infer cid.
                var segments = FileUri.Split('.');
                if (!segments[segments.Length - 2].StartsWith("c")) {
                    Console.WriteLine("Cannot infer CID from Bilibili.");
                    return 1;
                }

                Cid = segments[segments.Length - 2].Substring(1);
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

            var suffix = Group != null ? $"c{chat.Cid}-{Group}" : $"c{chat.Cid}";
            var segments = rawFile.ToString().Split(".");
            var skippedSegments = segments[segments.Length - 2] == suffix ? 2 : 1;
            var targetUri = $"{string.Join(".", segments.SkipLast(skippedSegments))}.{suffix}.xml";
            var target = new PimixFile(targetUri);
            target.Delete();
            target.Write(memoryStream);

            memoryStream.Dispose();

            return 0;
        }
    }

    class UpperCaseUtf8Encoding : UTF8Encoding {
        public override string WebName => base.WebName.ToUpper();
    }
}
