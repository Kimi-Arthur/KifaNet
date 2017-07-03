using System;
using CommandLine;
using Newtonsoft.Json;
using NLog;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands
{
    [Verb("upload", HelpText = "Upload file to a cloud location.")]
    class UploadCommand : FileUtilCommand
    {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        [Option('i', "id", HelpText = "ID for the uri.")]
        public string FileId { get; set; }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute()
        {
            var source = new PimixFile(FileUri, FileId);
            var locationsForSource = source.FileInfo.Locations;
            if (locationsForSource == null || !locationsForSource.Contains(FileUri))
            {
                Console.WriteLine("Source location is not found!");
                Console.WriteLine("Please run info command first.");
                return 1;
            }

            var destinationLocation = FileInformation.CreateLocation(source.Id);
            var destination = new PimixFile(destinationLocation, source.Id);
            source.Copy(destination);

            var compareResult = destination.GetDiff(FileProperties.AllVerifiable);
            if (compareResult.infoDiff == FileProperties.None)
            {
                FileInformation.AddLocation(source.Id, destinationLocation);
                return 0;
            }
            else
            {
                logger.Warn("Upload failed! The following fields differ: {0}", compareResult);
                logger.Warn(
                    "Expected data:\n{0}",
                    JsonConvert.SerializeObject(
                        compareResult.baseInfo.RemoveProperties(FileProperties.All ^ compareResult.infoDiff),
                        Formatting.Indented));
                logger.Warn(
                    "Actual data:\n{0}",
                    JsonConvert.SerializeObject(
                        compareResult.calculatedInfo.RemoveProperties(FileProperties.All ^ compareResult.infoDiff),
                        Formatting.Indented));
                return 2;
            }
        }
    }
}