using System;
using System.IO;
using CommandLine;
using Pimix.Api.Files;
using Pimix.Bilibili;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("comments", HelpText = "Get comments for the video from Bilibili.")]
    class GetCommentsCommand : SubUtilCommand {
        [Value(0, Required = true, HelpText = "Target file to get comments for.")]
        public string FileUri { get; set; }

        [Option('c', "cid", HelpText = "Bilibili cid for comments.")]
        public string Cid { get; set; }

        public override int Execute() {
            var chat = new BilibiliChat {Cid = Cid};
            var memoryStream = new MemoryStream();
            chat.RawDocument.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var lastDot = FileUri.LastIndexOf(".", StringComparison.Ordinal);
            var targetUri = $"{FileUri.Substring(0, lastDot)}.{Cid}.xml";
            var target = new PimixFile(targetUri);
            target.Write(memoryStream);

            return 0;
        }
    }
}
