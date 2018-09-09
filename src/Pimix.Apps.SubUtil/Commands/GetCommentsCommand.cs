using System;
using System.IO;
using CommandLine;
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
            StringWriter sw = new StringWriter();
            chat.RawDocument.Save(sw);
            Console.WriteLine(sw.ToString());
            return 0;
        }
    }
}
