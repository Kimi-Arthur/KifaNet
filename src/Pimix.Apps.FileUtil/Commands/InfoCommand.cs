using System;
using System.Collections.Generic;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands
{
    [Verb("info", HelpText = "Generate information of the specified file.")]
    class InfoCommand : FileUtilCommand
    {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        [Option('v', "verify-all", HelpText = "Verify all verifiable fields of the file along with updating info.")]
        public bool VerifyAll { get; set; } = false;

        [Option('f', "fields-to-verify", HelpText = "Fields to verify. Only 'Size' is verified by default.")]
        public string FieldsToVerify { get; set; } = "Size";

        public FileProperties FilePropertiesToVerify
            => VerifyAll ? FileProperties.AllVerifiable : FileProperties.AllVerifiable & (FileProperties)Enum.Parse(typeof(FileProperties), FieldsToVerify);

        [Option('u', "update", HelpText = "Whether to update result to server.")]
        public bool Update { get; set; } = false;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute()
        {
            var f = new PimixFile(FileUri);

            var info = f.GetInfo(FilePropertiesToVerify);
            var oldInfo = f.GetInfo(FileProperties.None, FileProperties.None);

            var compareResult = info.CompareProperties(oldInfo, FilePropertiesToVerify);
            if (compareResult == FileProperties.None)
            {
                if (Update)
                {
                    FileInformation.Patch(info);
                }
                else
                {
                    Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
                }

                return 0;
            }
            else
            {
                logger.Warn("Verify failed! The following fields differ: {0}", compareResult);
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
