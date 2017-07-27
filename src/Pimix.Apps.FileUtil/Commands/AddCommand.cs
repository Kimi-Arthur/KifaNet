using System;
using System.Collections.Generic;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands
{
    [Verb("add", HelpText = "Add file entry.")]
    class AddCommand : FileUtilCommand
    {
        [Value(0, Required = true, MetaName = "File URL")]
        public string FileUri { get; set; }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute()
        {
            var f = new PimixFile(FileUri);
            var result = f.Add();

            if (result.infoDiff == FileProperties.None)
            {
                logger.Info("Successfully added file.");
                logger.Info(JsonConvert.SerializeObject(FileInformation.Get(f.Id), Formatting.Indented));
                return 0;
            }
            else
            {
                logger.Warn("Conflict with old file info! Please check: {0}", result.infoDiff);
                logger.Warn(
                    "Expected data:\n{0}",
                    JsonConvert.SerializeObject(
                        result.baseInfo.RemoveProperties(FileProperties.All ^ result.infoDiff),
                        Formatting.Indented));
                logger.Warn(
                    "Actual data:\n{0}",
                    JsonConvert.SerializeObject(
                        result.calculatedInfo.RemoveProperties(FileProperties.All ^ result.infoDiff),
                        Formatting.Indented));
                return 1;
            }
        }
    }
}
