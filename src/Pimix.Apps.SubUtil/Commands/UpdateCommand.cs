using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Subtitle.Ass;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("update", HelpText = "Update subtitle with given modification.")]
    class UpdateCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to update.")]
        public string FileUri { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var sub = AssDocument.Parse(target.OpenRead());

            var actions = new List<Action> {new TimeShiftAction()};
            SelectOne(actions, choiceName: "actions").Update(sub);

            logger.Info(sub.ToString());
            target.Delete();
            target.Write(sub.ToString());
            return 0;
        }
    }

    abstract class Action {
        abstract public void Update(AssDocument sub);
    }

    class TimeShiftAction : Action {
        public override void Update(AssDocument sub) {
            var selectedLines = PimixCommand.SelectMany(
                sub.Sections.OfType<AssEventsSection>().First().Events.ToList());
            var shift = PimixCommand.Confirm("Input the amount of time to shift")
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
}
