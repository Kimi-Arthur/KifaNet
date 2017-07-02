using System;
using CommandLine;
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

        public override int Execute()
        {
            var source = new PimixFile(FileUri, FileId);
            var locationsForSource = source.FileInfo.Locations;
            if (locationsForSource == null || !locationsForSource.Contains(FileUri)) {
                Console.WriteLine("Source location is not found!");
                Console.WriteLine("Please run info command first.");
                return 1;
            }

            var destinationLocation = FileInformation.CreateLocation(source.Id);
            var destination = new PimixFile(destinationLocation, source.Id);
            source.Copy(destination);

            FileInformation.AddLocation(source.Id, destinationLocation);

            return 0;
        }
    }
}