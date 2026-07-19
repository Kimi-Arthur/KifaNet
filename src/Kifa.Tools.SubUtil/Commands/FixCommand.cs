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

    [Value(0, Required = true, HelpText = "Target subtitle files to fix.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var filesToFix = KifaFile.FindExistingFiles(FileNames, pattern: "*.ass")
            .Where(file => !file.Path.Split('/').Contains("Sources"))
            .ToList();

        var selected = SelectMany(filesToFix, file => file.ToString(),
            "subtitle files to fix");
        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("subtitle files to fix", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value) {
            ExecuteItem($"Fix subtitle file {file}", () => FixSubtitle(file));
        }

        return LogSummary();
    }

    static KifaActionResult FixSubtitle(KifaFile file) {
        if (file.Extension != "ass") {
            return KifaActionResult.BadRequest("Only ass files are supported.");
        }

        var targetFile = FixFilename(file);

        var sub = AssDocument.Parse(targetFile.OpenRead());
        sub = FixSubtitleResolution(sub);
        // This is needed as Emby will reject the parts with \fad element.
        sub = RemoveFadElement(sub);
        sub = FixStyles(sub);
        sub = FixTitle(sub, targetFile);

        targetFile.Delete();
        targetFile.Write(sub.ToString());
        return KifaActionResult.Success();
    }

    static KifaFile FixFilename(KifaFile file) {
        var name = file.Name;
        var newName = name;

        // Fix dual language codes e.g. .zh-en. -> .zh.
        if (newName.Contains(".zh-en.")) {
            newName = newName.Replace(".zh-en.", ".zh.");
        }

        // Fix old .default.ass naming -> .<bilibili>.zh.ass
        if (newName.EndsWith(".default.ass")) {
            newName = newName.Replace(".default.ass", ".<bilibili>.zh.ass");
        }

        if (newName != name) {
            var targetFile = file.Parent.GetFile(newName);
            Logger.Info($"Renaming subtitle file: {file.Name} => {targetFile.Name}");
            file.Move(targetFile);
            return targetFile;
        }

        return file;
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

    static AssDocument FixTitle(AssDocument sub, KifaFile file) {
        if (sub.Sections.FirstOrDefault(s => s is AssScriptInfoSection) is AssScriptInfoSection header) {
            if (string.IsNullOrEmpty(header.Title) || header.Title == "Untitled") {
                header.Title = file.BaseName;
            }
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
