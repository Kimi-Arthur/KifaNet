using System.Collections.Generic;
using System.Linq;
using NLog;

namespace Pimix.Subtitle.Ass {
    public class AssStylesSection : AssSection {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string SectionHeader = "[V4+ Styles]";
        public override string SectionTitle => SectionHeader;

        public List<string> Format
            => new List<string> {
                "Name",
                "Fontname",
                "Fontsize",
                "PrimaryColour",
                "SecondaryColour",
                "OutlineColour",
                "BackColour",
                "Bold",
                "Italic",
                "Underline",
                "StrikeOut",
                "ScaleX",
                "ScaleY",
                "Spacing",
                "Angle",
                "BorderStyle",
                "Outline",
                "Shadow",
                "Alignment",
                "MarginL",
                "MarginR",
                "MarginV",
                "Encoding"
            };

        // TODO: solve sync problem between these two.
        public List<AssStyle> Styles { get; set; } = new List<AssStyle>();

        public Dictionary<string, AssStyle> NamedStyles { get; set; } =
            new Dictionary<string, AssStyle>();

        public static AssStylesSection Parse(IEnumerable<string> lines) {
            var section = new AssStylesSection();
            List<string> headers = null;
            foreach (var line in lines) {
                if (line.Contains(": ")) {
                    var segments = line.Split(": ");
                    switch (segments[0]) {
                        case "Format":
                            headers = segments[1].Split(",").Select(s => s.Trim()).ToList();
                            break;
                        case "Style":
                            if (headers == null) {
                                logger.Warn(
                                    "Should see header line before style line in style section.");
                                break;
                            }

                            var style = AssStyle.Parse(segments[1].Split(",").Select(s => s.Trim()),
                                headers);
                            section.NamedStyles[style.ValidName] = style;
                            section.Styles.Add(style);
                            break;
                    }
                }
            }

            return section;
        }

        public override IEnumerable<AssLine> AssLines {
            get {
                yield return new AssLine("Format", Format);
                foreach (var style in Styles) {
                    yield return style;
                }
            }
        }
    }
}
