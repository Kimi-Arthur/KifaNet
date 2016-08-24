using System;
using System.Collections.Generic;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using Pimix.IO;

namespace Pimix.Apps.FileUtil
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

            using (var stream = f.OpenRead())
            {
                var info = FileInformation.Get(f.Path).RemoveProperties(FilePropertiesToVerify).AddProperties(stream, FileProperties.All);
                info.Path = f.Path;
                if (info.Locations == null)
                    info.Locations = new Dictionary<string, string>();
                info.Locations[f.Spec] = f.ToString();
                var old = FileInformation.Get(f.Path);
                var compareResult = info.CompareProperties(old, FilePropertiesToVerify);
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
                    logger.Warn("Verify failed! The following fields differ:");
                    logger.Warn("\t" + compareResult);
                    logger.Warn("");
                    logger.Warn("Expected data:");
                    logger.Warn(JsonConvert.SerializeObject(old.RemoveProperties(FileProperties.All ^ compareResult), Formatting.Indented));
                    logger.Warn("");
                    logger.Warn("Actual data:");
                    logger.Warn(JsonConvert.SerializeObject(info.RemoveProperties(FileProperties.All ^ compareResult), Formatting.Indented));
                    logger.Warn("");
                    return 1;
                }
            }
        }
    }
}
