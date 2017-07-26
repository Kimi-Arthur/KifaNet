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

        private FileProperties FilePropertiesToVerify = FileProperties.AllVerifiable;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute()
        {
            var f = new PimixFile(FileUri);

            var info = f.CalculateInfo(FilePropertiesToVerify | FileProperties.EncryptionKey);
            var sha256Info = FileInformation.Get($"/$/{info.SHA256}");

            if (f.FileInfo.SHA256 == null && sha256Info.SHA256 == info.SHA256) {
                // One same file already exists.
                FileInformation.Link(sha256Info.Id, info.Id);
            }

            var oldInfo = f.FileInfo;

            var compareResult = info.CompareProperties(oldInfo, FilePropertiesToVerify);
            if (compareResult == FileProperties.None)
            {
                info.EncryptionKey = oldInfo.EncryptionKey ?? info.EncryptionKey;  // Only happens for unencrypted file.

                FileInformation.Patch(info);
                FileInformation.AddLocation(f.Id, FileUri);

                logger.Info("Successfully added file.");
                logger.Info(JsonConvert.SerializeObject(FileInformation.Get(f.Id), Formatting.Indented));
                return 0;
            }
            else
            {
                logger.Warn("Conflict with old file info! Please check: {0}", compareResult);
                logger.Warn(
                    "Expected data:\n{0}",
                    JsonConvert.SerializeObject(
                        oldInfo.RemoveProperties(FileProperties.All ^ compareResult),
                        Formatting.Indented));
                logger.Warn(
                    "Actual data:\n{0}",
                    JsonConvert.SerializeObject(
                        info.RemoveProperties(FileProperties.All ^ compareResult),
                        Formatting.Indented));
                return 1;
            }
        }
    }
}
