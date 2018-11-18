using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Subtitle.Ass;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("fix", HelpText = "Fix subtitle.")]
    class FixSubtitleCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to normalize subtitle for.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var sub = FixSubtitleResolution(AssDocument.Parse(target.OpenRead()));
            Console.WriteLine(sub.ToString());
            return 0;
        }

        AssDocument FixSubtitleResolution(AssDocument sub) {
            if (sub.Sections.FirstOrDefault(s => s is AssScriptInfoSection) is AssScriptInfoSection header) {
                var scriptHeight = header.PlayResY > 0
                    ? header.PlayResY
                    : AssScriptInfoSection.DefaultPlayResY;

                if (scriptHeight == AssScriptInfoSection.PreferredPlayResY) {
                    return sub;
                }

                header.PlayResX = AssScriptInfoSection.PreferredPlayResX;
                header.PlayResY = AssScriptInfoSection.PreferredPlayResY;
                
                var scale = AssScriptInfoSection.PreferredPlayResY * 1.0 / scriptHeight;

                foreach (var styleSection in sub.Sections.Where(s => s is AssStylesSection)) {
                    foreach (var line in styleSection.AssLines) {
                        if (line is AssStyle styleLine) {
                            styleLine.Scale(scale);
                        }
                    }
                }
            }

            return sub;
        }
    }
}
