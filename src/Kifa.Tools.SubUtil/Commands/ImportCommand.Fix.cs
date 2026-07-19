using System.Linq;
using Kifa.Api.Files;
using Kifa.Subtitle.Ass;

namespace Kifa.Tools.SubUtil.Commands;

partial class ImportCommand {
    static void FixSubtitle(KifaFile file, string title, string releaseId) {
        if (file.Extension != "ass") {
            return;
        }

        var sub = AssDocument.Parse(file.OpenRead());
        sub = FixSubtitleResolution(sub);
        sub = RemoveFadElement(sub);
        sub = FixStyles(sub);
        sub = FixTitle(sub, title, releaseId);

        file.Delete();
        file.Write(sub.ToString());
    }

    static AssDocument FixStyles(AssDocument sub) {
        var styleSection = sub.Sections.OfType<AssStylesSection>().FirstOrDefault();
        if (styleSection == null) {
            styleSection = new AssStylesSection {
                Styles = AssStyle.Styles
            };
            sub.Sections.Insert(1, styleSection);
        } else {
            var existingStyleNames = styleSection.Styles.Select(s => s.Name).ToHashSet();
            foreach (var standardStyle in AssStyle.Styles) {
                if (!existingStyleNames.Contains(standardStyle.Name)) {
                    styleSection.Styles.Add(standardStyle);
                }
            }
        }

        return sub;
    }

    static AssDocument FixTitle(AssDocument sub, string title, string releaseId) {
        if (sub.Sections.FirstOrDefault(s => s is AssScriptInfoSection) is AssScriptInfoSection header) {
            header.Title = title;
            header.OriginalScript = releaseId;
        }

        return sub;
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
