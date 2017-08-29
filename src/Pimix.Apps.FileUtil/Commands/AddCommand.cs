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

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var f = new PimixFile(FileUri);
            if (!f.Exists()) {
                logger.Error$"Source {0} doesn't exist!", f);
                return 1;
            }

            logger.Info("Adding {0}...", f);
            var result = f.Add();

            if (result.infoDiff == FileProperties.None) {
                logger.Info("Successfully added {0}", f);
                logger.Info(JsonConvert.SerializeObject(f.FileInfo, Formatting.Indented));
                return 0;
            } else {
                logger.Warn("Conflict with old file info! Please check: {0}", result.infoDiff);
                return 1;
            }
        }
    }
}
