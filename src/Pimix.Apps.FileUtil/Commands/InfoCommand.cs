using System;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("info", HelpText = "Generate information of the specified file.")]
    class InfoCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true)]
        public string FileUri { get; set; }

        [Option('i', "id", HelpText = "ID for the uri.")]
        public string FileId { get; set; }

        [Option('v', "verify-all", HelpText =
            "Verify all verifiable fields of the file along with updating info.")]
        public bool VerifyAll { get; set; }

        [Option('f', "fields-to-verify", HelpText =
            "Fields to verify. Only 'Size' is verified by default.")]
        public string FieldsToVerify { get; set; } = "Size";

        public FileProperties FilePropertiesToVerify
            => VerifyAll
                ? FileProperties.AllVerifiable
                : FileProperties.AllVerifiable &
                  (FileProperties) Enum.Parse(typeof(FileProperties), FieldsToVerify);

        [Option('u', "update", HelpText = "Whether to update result to server.")]
        public bool Update { get; set; } = false;

        public override int Execute() {
            var f = new PimixFile(FileUri, FileId);

            if (!f.Exists()) {
                logger.Error("File {0} doesn't exist.", f);
                return 1;
            }

            var info = f.CalculateInfo(FilePropertiesToVerify | FileProperties.EncryptionKey);
            var oldInfo = f.FileInfo;

            var compareResult = info.CompareProperties(oldInfo, FilePropertiesToVerify);
            if (compareResult != FileProperties.None) {
                logger.Warn("Verify failed! The following fields differ: {0}", compareResult);
                logger.Warn("Expected data:\n{0}",
                    JsonConvert.SerializeObject(oldInfo.RemoveProperties(FileProperties.All ^ compareResult),
                        Defaults.PrettyJsonSerializerSettings));
                logger.Warn("Actual data:\n{0}",
                    JsonConvert.SerializeObject(info.RemoveProperties(FileProperties.All ^ compareResult),
                        Defaults.PrettyJsonSerializerSettings));
                return 1;
            }

            if (Update) {
                FileInformation.Client.Update(info);
                FileInformation.Client.AddLocation(f.Id, FileUri, true);
            }

            Console.WriteLine(JsonConvert.SerializeObject(info, Defaults.PrettyJsonSerializerSettings));

            return 0;
        }
    }
}
