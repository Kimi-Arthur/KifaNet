using System.Collections.Generic;
using System.Linq;
using NLog;

namespace Pimix.Subtitle.Ass {
    public class AssEventsSection : AssSection {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string SectionHeader = "[Events]";
        public override string SectionTitle => SectionHeader;

        public List<string> Format
            => new List<string> {
                "Layer",
                "Start",
                "End",
                "Style",
                "Name",
                "MarginL",
                "MarginR",
                "MarginV",
                "Effect",
                "Text"
            };

        public List<AssEvent> Events { get; set; } = new List<AssEvent>();

        public static AssEventsSection Parse(AssStylesSection stylesSection,
            IEnumerable<string> lines) {
            var section = new AssEventsSection();
            List<string> headers = null;
            foreach (var line in lines) {
                var separatorIndex = line.IndexOf(AssLine.Separator);
                if (separatorIndex >= 0) {
                    var type = line.Substring(0, separatorIndex);
                    var content = line.Substring(separatorIndex + 1).Trim();

                    switch (line.Substring(0, separatorIndex)) {
                        case "Format":
                            headers = content.Split(",").Select(s => s.Trim()).ToList();
                            break;
                        case "Dialogue":
                            if (headers == null) {
                                logger.Warn(
                                    "Should see header line before event line in events section.");
                                break;
                            }

                            section.Events.Add(AssEvent.Parse(stylesSection.NamedStyles, type,
                                content.Split(",", headers.Count).Select(s => s.Trim()), headers));
                            break;
                    }
                }
            }

            return section;
        }

        public override IEnumerable<AssLine> AssLines {
            get {
                yield return new AssLine("Format", Format);
                foreach (var evt in Events) {
                    yield return evt;
                }
            }
        }
    }
}
