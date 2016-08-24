using System;
using System.Threading;
using CommandLine;
using Pimix.IO;

namespace Pimix.Apps.FileUtil
{
    [Verb("cp", HelpText = "Copy file from SOURCE to DEST.")]
    class CopyCommand : FileUtilCommand
    {
        [Value(0, Required = true)]
        public string SourceUri { get; set; }

        [Value(1, Required = true)]
        public string DestinationUri { get; set; }

        [Option('p', "precheck", HelpText = "Whether to check (and update) SOURCE before copying.")]
        public bool Precheck { get; set; } = false;

        [Option('d', "destination-check", HelpText = "Whether to check (and update) DEST before copying.")]
        public bool DestinationCheck { get; set; } = true;

        [Option('u', "update", HelpText = "Whether to update result to server after copying.")]
        public bool Update { get; set; } = false;

        [Option('v', "verify-all", HelpText = "Verify all verifiable fields before update.")]
        public bool VerifyAll { get; set; } = false;

        [Option('f', "fields-to-verify", HelpText = "Fields to verify. Only 'Size' is verified by default.")]
        public string FieldsToVerify { get; set; } = "Size";

        public override int Execute()
        {
            var source = new PimixFile(SourceUri);
            var destination = new PimixFile(DestinationUri);

            if (NeedsPrecheck(source))
            {
                var result = new InfoCommand { Update = true, VerifyAll = true, FileUri = SourceUri }.Execute();
                if (result != 0)
                {
                    Console.Error.WriteLine("Precheck failed!");
                    return 1;
                }
            }

            if (DestinationCheck)
            {
                try
                {
                    var result = new InfoCommand { Update = true, VerifyAll = VerifyAll, FieldsToVerify = FieldsToVerify, FileUri = DestinationUri }.Execute();
                    if (result == 0)
                    {
                        return 0;
                    }
                }
                catch
                {
                    // Ignore the error for now.
                }
            }

            new PimixFile(SourceUri).Copy(new PimixFile(DestinationUri));

            // Wait 5 seconds to ensure server sync.
            Thread.Sleep(TimeSpan.FromSeconds(5));

            return Update ? new InfoCommand { Update = true, VerifyAll = VerifyAll, FieldsToVerify = FieldsToVerify, FileUri = DestinationUri }.Execute() : 0;
        }

        bool NeedsPrecheck(PimixFile file)
        {
            var info = FileInformation.Get(file.Path);
            return !info.GetProperties().HasFlag(FileProperties.All) || !info.Locations.ContainsKey(file.Spec);
        }
    }
}
