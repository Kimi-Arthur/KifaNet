using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("touch", HelpText = "Touch file.")]
    class TouchCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, MetaName = "File URL")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            if (target.Client == null) {
                Console.WriteLine($"Target {FileUri} not accessible. Wrong server?");
                return 1;
            }

            var files = FileInformation.Client.ListFolder(target.Id, true);
            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                Console.Write($"Confirm touching the {files.Count} files above?");
                Console.ReadLine();

                return files.Select(f => TouchFile(new PimixFile(target.Host + f))).Max();
            }

            return TouchFile(target);
        }

        int TouchFile(PimixFile target) {
            if (target.Exists()) {
                logger.Info($"{target} already exists!");
                return 0;
            }

            target.Touch();

            if (target.Exists()) {
                logger.Info($"{target} is successfully touched!");
                return 0;
            }

            logger.Fatal($"{target} doesn't exist unexpectedly!");
            return 2;
        }
    }
}
