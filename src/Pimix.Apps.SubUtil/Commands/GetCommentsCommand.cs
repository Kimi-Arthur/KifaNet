using System;
using System.IO;
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

        public override int Execute() {
            if (Aid != null) {
                var status = 0;
                var ids = Aid.Split('p');
                var v = BilibiliVideo.Get(ids[0]);
                foreach (var page in v.Pages) {
                    logger.Warn($"{v.Title} {page.Id} {page.Cid} {page.Title}");
                }

                return status;
            }

            return GetChat(new BilibiliChat {Cid = Cid});
        }

        int GetChat(BilibiliChat chat) {
            var memoryStream = new MemoryStream();
            var writer = new XmlTextWriter(memoryStream, new UpperCaseUtf8Encoding()) {
                Formatting = Formatting.Indented
            };
            chat.RawDocument.Save(writer);

            memoryStream.Seek(0, SeekOrigin.Begin);

            var lastDot = FileUri.LastIndexOf(".", StringComparison.Ordinal);
            var targetUri = $"{FileUri.Substring(0, lastDot)}.{Cid}.xml";
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
