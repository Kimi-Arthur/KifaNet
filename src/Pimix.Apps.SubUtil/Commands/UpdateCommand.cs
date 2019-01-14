using System;
using System.IO;
using System.Text;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.Subtitle.Ass;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("up", HelpText = "Update subtitle with given modification.")]
    class UpdateCommand : SubUtilCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file to update.")]
        public string FileUri { get; set; }

        [Option('t', "time", HelpText = "Shift subtitles' time by given shift.")]
        public string TimeShift { get; set; }

        public override int Execute() {
            var target = new PimixFile(FileUri);
            var sub = AssDocument.Parse(target.OpenRead());

            Console.Write(TimeShift);
            var shift = TimeSpan.Parse(TimeShift);
            foreach (var section in sub.Sections) {
                if (section is AssEventsSection) {
                    foreach (var line in section.AssLines) {
                        if (line is AssDialogue dialogue) {
                            dialogue.Start += shift;
                            dialogue.End += shift;
                        }
                    }
                }
            }
            
            logger.Info(sub.ToString());
            target.Delete();
            target.Write(new MemoryStream(new UTF8Encoding(false).GetBytes(sub.ToString())));
            return 0;
        }
    }
}
