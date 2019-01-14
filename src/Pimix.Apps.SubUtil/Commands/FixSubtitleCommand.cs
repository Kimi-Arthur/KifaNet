using System;
using System.IO;
using System.Linq;
using System.Text;
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
            var sub = AssDocument.Parse(target.OpenRead());
            sub = FixSubtitleResolution(sub);
            Console.WriteLine(sub.ToString());
            target.Delete();
            target.Write(new MemoryStream(new UTF8Encoding(false).GetBytes(sub.ToString())));
            return 0;
        }

        AssDocument FixSubtitleResolution(AssDocument sub) {
            if (!(sub.Sections.FirstOrDefault(s => s is AssScriptInfoSection) is
                AssScriptInfoSection header)) {
                return sub;
            }

            var scriptHeight = header.PlayResY > 0
                ? header.PlayResY
                : AssScriptInfoSection.DefaultPlayResY;

            if (scriptHeight == AssScriptInfoSection.PreferredPlayResY) {
                return sub;
            }

            header.PlayResX = AssScriptInfoSection.PreferredPlayResX;
            header.PlayResY = AssScriptInfoSection.PreferredPlayResY;

            var scale = AssScriptInfoSection.PreferredPlayResY * 1.0 / scriptHeight;
            logger.Info("Scale by {0}", scale);

            foreach (var styleSection in sub.Sections.Where(s => s is AssStylesSection)) {
                foreach (var line in styleSection.AssLines) {
                    if (line is AssStyle styleLine) {
                        styleLine.Scale(scale);
                    }
                }
            }

            foreach (var eventsSection in sub.Sections.Where(s => s is AssEventsSection)) {
                foreach (var line in eventsSection.AssLines) {
                    if (line is AssDialogue dialogue) {
                        foreach (var element in dialogue.Text.TextElements) {
                            if (element is AssDialogueControlTextElement controlTextElement) {
                                foreach (var e in controlTextElement.Elements) {
                                    e.Scale(scale);
                                }
                            }
                        }
                    }
                }
            }

            return sub;
        }
    }
}
