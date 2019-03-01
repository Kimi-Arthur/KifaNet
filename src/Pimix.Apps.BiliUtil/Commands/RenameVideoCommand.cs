using System;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil.Commands {
    [Verb("rename", HelpText = "Rename video file to comply.")]
    class RenameVideoCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Regex fileNamePattern = new Regex(@"^AV(\d+) P(\d+) .* cid (\d+)$");

        [Value(0, Required = true, HelpText = "Target file to rename.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var newName = getDesiredFileName(target.BaseName);
            if (newName == null) {
                logger.Info($"No need to rename {target}");
                return 0;
            }

            while (true) {
                var newTarget = new PimixFile(FileUri) {BaseName = newName};

                Console.WriteLine($"Confirm renaming\n{target}\nto\n{newTarget}?");
                var line = Console.ReadLine();
                if (line == "") {
                    target.Move(newTarget);
                    break;
                }

                newName = line;
            }

            return 0;
        }

        string getDesiredFileName(string targetName) {
            var match = fileNamePattern.Match(targetName);
            if (!match.Success) {
                logger.Warn("Pattern mismatch.");
                return null;
            }

            var aid = $"av{match.Groups[1].Value}";
            var pid = int.Parse(match.Groups[2].Value);
            var cid = match.Groups[3].Value;

            PimixService.Patch(new BilibiliVideo {Id = aid});
            var v = PimixService.Get<BilibiliVideo>(aid);
            var p = v.Pages.First(x => x.Id == pid);

            if (cid != p.Cid) {
                logger.Warn("CID mismatch.");
                return null;
            }

            return v.Pages.Count > 1
                ? $"{v.Title} P{pid} {p.Title}-{aid}p{pid}.c{cid}"
                : $"{v.Title} {p.Title}-{aid}.c{cid}";
        }
    }
}
