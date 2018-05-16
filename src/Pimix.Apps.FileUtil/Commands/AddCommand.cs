using CommandLine;
using Newtonsoft.Json;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("add", HelpText = "Add file entry.")]
    class AddCommand : FileUtilCommand {
        [Value(0, Required = true, MetaName = "File URL")]
        public string FileUri { get; set; }

        [Option('f', "force-check", HelpText =
            "Check file integrity even if it is already recorded.")]
        public bool ForceRecheck { get; set; } = false;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var f = new PimixFile(FileUri);
            if (!f.Exists()) {
                logger.Error("Source {0} doesn't exist!", f);
                return 1;
            }

            logger.Info("Adding {0}...", f);
            var result = f.Add(ForceRecheck);

            if (result == FileProperties.None) {
                logger.Info("Successfully added {0}", f);
                logger.Info(JsonConvert.SerializeObject(f.FileInfo, Formatting.Indented));
                return 0;
            }

            logger.Warn("Conflict with old file info! Please check: {0}", result);
            return 1;
        }
    }
}
