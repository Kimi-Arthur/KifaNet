using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Subtitle.Ass;
using NLog;

namespace Kifa.Tools.SubUtil.Commands; 

[Verb("update", HelpText = "Update subtitle with given modification.")]
class UpdateCommand : KifaCommand {
    const string SubtitlesPrefix = "/Subtitles";
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file to update.")]
    public string FileUri { get; set; }

    public override int Execute() {
        var target = new KifaFile(FileUri);
        if (!target.Path.StartsWith(SubtitlesPrefix)) {
            target = target.GetFilePrefixed(SubtitlesPrefix);
        }

        var sub = AssDocument.Parse(target.OpenRead());

        SelectOne(new List<Action> {new TimeShiftAction()}, choiceName: "actions").choice.Update(sub);

        logger.Info(sub.ToString());
        target.Delete();
        target.Write(sub.ToString());
        return 0;
    }
}

abstract class Action {
    public abstract void Update(AssDocument sub);
}

class TimeShiftAction : Action {
    public override void Update(AssDocument sub) {
        var selectedLines = KifaCommand.SelectMany(
            sub.Sections.OfType<AssEventsSection>().First().Events.ToList());
        var shift = KifaCommand.Confirm("Input the amount of time to shift").ParseTimeSpanString();
        ShiftTime(selectedLines, shift);
    }

    static void ShiftTime(IEnumerable<AssEvent> selectedLines, TimeSpan shift) {
        foreach (var line in selectedLines) {
            line.Start += shift;
            line.End += shift;
        }
    }

    public override string ToString() => "Shift subtitles in time.";
}