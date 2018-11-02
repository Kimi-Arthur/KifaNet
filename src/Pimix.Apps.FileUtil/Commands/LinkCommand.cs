using System;
using CommandLine;
using NLog;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("ln", HelpText = "Create a link to TARGET with the name LINK_NAME.")]
    class LinkCommand : FileUtilCommand {
        [Value(0, MetaName = "TARGET", MetaValue = "STRING", Required = true,
            HelpText = "The target for this link.")]
        public string Target { get; set; }

        [Value(1, MetaName = "LINK_NAME", MetaValue = "STRING", Required = true,
            HelpText = "The link's name.")]
        public string LinkName { get; set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            if (!Target.StartsWith("/") || !LinkName.StartsWith("/")) {
                Console.Error.WriteLine("You should use absolute file path for the two arguments.");
                return 1;
            }

            var result = PimixService.Link<FileInformation>(Target, LinkName);
            if (result) {
                logger.Info("Successfully linked {0} with {1}!", LinkName, Target);
            } else {
                logger.Fatal("Linking {0} to {1} is unsuccessful!", LinkName, Target);
            }

            return result ? 0 : 1;
        }
    }
}
