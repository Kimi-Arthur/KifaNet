using System;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Pimix.IO;

namespace Kifa.Tools.BiliUtil.Commands {
    [Verb("link", HelpText = "Link video file to proper location.")]
    class LinkVideoCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to rename.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new KifaFile(FileUri).Id;
            var newName = GetDesiredFileName(target);
            if (newName == null) {
                logger.Info($"No need to rename {target}");
                return 0;
            }

            while (true) {
                Console.WriteLine($"Confirm renaming\n{target}\nto\n{newName}?");
                var line = Console.ReadLine();
                if (line == "") {
                    FileInformation.Client.Link(target, newName);
                    break;
                }

                newName = line;
            }

            return 0;
        }

        string GetDesiredFileName(string targetName) {
            if (targetName.StartsWith("/Venus/bilibili/")) {
                var segments = targetName.Split('/');
                segments[2] = "Dancing";
                segments[3] = string.Join("-", segments[3].Split('-').SkipLast(1));
                segments[4] = string.Join("-", segments[4].Split('-').SkipLast(1)) + "." +
                              segments[4].Split('.').Last();
                return string.Join("/", segments);
            }

            return null;
        }
    }
}
