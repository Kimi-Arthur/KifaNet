using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("ln", HelpText = "Create a link to TARGET with the name LINK_NAME.")]
    class LinkCommand : PimixCommand {
        [Value(0, MetaName = "TARGET", MetaValue = "STRING", Required = true,
            HelpText = "The target for this link.")]
        public string Target { get; set; }

        [Value(1, MetaName = "LINK_NAME", MetaValue = "STRING", Required = true,
            HelpText = "The link's name.")]
        public string LinkName { get; set; }

        [Option('i', "id", HelpText =
            "Treat all file names as id. Note that linking is always about conceptual files.")]
        public bool ById { get; set; } = false;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            if (ById) {
                return LinkFile(Target, LinkName);
            }

            return LinkFile(new PimixFile(Target).Id, new PimixFile(LinkName).Id);
        }

        static int LinkFile(string target, string linkName) {
            if (!target.StartsWith("/") || !linkName.StartsWith("/")) {
                logger.Error("You should use absolute file path for the two arguments.");
                return 1;
            }

            PimixService.Link<FileInformation>(target, linkName);
            logger.Info("Successfully linked {0} with {1}!", linkName, target);

            return 0;
        }
    }
}
