using System;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Subtitle.Ass;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("fix", HelpText = "Fix subtitle.")]
class FixCommand : KifaFileCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override string Prefix => "/Subtitles";

    protected override int ExecuteOneKifaFile(KifaFile file) {
        if (!file.Path.EndsWith(".ass")) {
            return 0;
        }

        var sub = AssDocument.Parse(file.OpenRead());
        sub = FixSubtitleResolution(sub);
        // This is needed as Emby will reject the parts with \fad element.
        sub = RemoveFadElement(sub);
        Console.WriteLine(sub.ToString());
        file.Delete();
        file.Write(sub.ToString());
        return 0;
    }

    static AssDocument FixSubtitleResolution(AssDocument sub) {
        if (!(sub.Sections.FirstOrDefault(s => s is AssScriptInfoSection) is AssScriptInfoSection
                header)) {
            return sub;
        }

        var scriptHeight =
            header.PlayResY > 0 ? header.PlayResY : AssScriptInfoSection.DefaultPlayResY;

        var scriptWidth =
            header.PlayResX > 0 ? header.PlayResX : AssScriptInfoSection.DefaultPlayResX;

        if (scriptWidth == AssScriptInfoSection.PreferredPlayResX &&
            scriptHeight == AssScriptInfoSection.PreferredPlayResY) {
            return sub;
        }

        header.PlayResX = AssScriptInfoSection.PreferredPlayResX;
        header.PlayResY = AssScriptInfoSection.PreferredPlayResY;

        var scaleX = AssScriptInfoSection.PreferredPlayResX * 1.0 / scriptWidth;
        var scaleY = AssScriptInfoSection.PreferredPlayResY * 1.0 / scriptHeight;
        Logger.Info("Scale by {0}", scaleY);

        foreach (var styleSection in sub.Sections.Where(s => s is AssStylesSection)) {
            foreach (var line in styleSection.AssLines) {
                if (line is AssStyle styleLine) {
                    styleLine.Scale(scaleY);
                }
            }
        }

        foreach (var eventsSection in sub.Sections.Where(s => s is AssEventsSection)) {
            foreach (var line in eventsSection.AssLines) {
                if (line is AssDialogue dialogue) {
                    foreach (var element in dialogue.Text.TextElements) {
                        if (element is AssDialogueControlTextElement controlTextElement) {
                            foreach (var e in controlTextElement.Elements) {
                                e.Scale(scaleX, scaleY);
                            }
                        }
                    }
                }
            }
        }

        return sub;
    }

    static AssDocument RemoveFadElement(AssDocument sub) {
        foreach (var eventsSection in sub.Sections.Where(s => s is AssEventsSection)) {
            foreach (var line in eventsSection.AssLines) {
                if (line is AssDialogue dialogue) {
                    foreach (var element in dialogue.Text.TextElements) {
                        if (element is AssDialogueControlTextElement controlTextElement) {
                            controlTextElement.Elements = controlTextElement.Elements
                                .Where(e => e is not FadeTimeFunction).ToList();
                        }
                    }
                }
            }
        }

        return sub;
    }
}
