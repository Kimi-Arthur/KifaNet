using CommandLine;
using NLog;
using Pimix.Api.Files;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("normalize", HelpText = "Normalize subtitle.")]
    class NormalizeSubtitleCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to normalize subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            return NormalizeSubtitle(target);
        }

        int NormalizeSubtitle(PimixFile target) {
            return 0;
        }
    }
}
