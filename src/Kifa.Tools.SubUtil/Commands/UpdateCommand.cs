using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Subtitle.Ass;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("update", HelpText = "Update subtitle with given modification.")]
class UpdateCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file to update.")]
    public string FileUri { get; set; }

    public override int Execute(KifaTask? task = null) {
        var target = new KifaFile(FileUri).GetSubtitleFile();

        var sub = AssDocument.Parse(target.OpenRead());

        SelectOne(new List<Action> {
            new TimeShiftAction()
        }, choiceName: "actions").Value.Choice.Update(sub);

        Logger.Info(sub.ToString());
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
        var shift = KifaCommand.Confirm("Input the amount of time to shift:", "10s")
            .ParseTimeSpanString();
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
