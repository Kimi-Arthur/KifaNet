using System;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Subtitle.Ass;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("normalize", HelpText = "Normalize subtitle.")]
    class NormalizeSubtitleCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to normalize subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var sub = NormalizeSubtitle(AssDocument.Parse(target.OpenRead()));
            Console.WriteLine(sub.ToString());
            return 0;
        }

        AssDocument NormalizeSubtitle(AssDocument sub) {
            return sub;
        }
    }
}
