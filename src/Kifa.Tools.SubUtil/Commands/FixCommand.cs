using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.Subtitle.Ass;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("fix", HelpText = "Fix subtitle. This includes the function of subx clean.")]
class FixCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target subtitle files to clean up.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var selected = SelectMany(KifaFile.FindExistingFiles(FileNames));
        foreach (var file in selected) {
            ExecuteItem(file.ToString(), () => FixSubtitle(file));
        }

        return LogSummary();
    }

    KifaActionResult FixSubtitle(KifaFile file) {
        if (file.Extension != "ass") {
            return new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message = "Only ass files are supported."
            };
        }

        var sub = AssDocument.Parse(file.OpenRead());
        sub = FixSubtitleResolution(sub);
        // This is needed as Emby will reject the parts with \fad element.
        sub = RemoveFadElement(sub);
        file.Delete();
        file.Write(sub.ToString());
        return KifaActionResult.Success;
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
        Logger.Debug(
            $"Scale by {scaleX} x {scaleY} ({scriptWidth} -> {AssScriptInfoSection.PreferredPlayResX}, {scriptHeight} -> {AssScriptInfoSection.PreferredPlayResY})");

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

        Logger.Debug("Removed all fad elements if any.");

        return sub;
    }
}
