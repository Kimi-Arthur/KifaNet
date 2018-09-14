using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public class AssEventsSection : AssSection {
        public override string SectionTitle { get; } = "Events";

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
