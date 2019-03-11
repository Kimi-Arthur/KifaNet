using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public class AssScriptInfoSection : AssSection {
        public const int DefaultPlayResX = 384;
        public const int DefaultPlayResY = 288;

        public const int PreferredPlayResX = 1920;
        public const int PreferredPlayResY = 1080;

        public const string SectionHeader = "[Script Info]";
        public override string SectionTitle => SectionHeader;

        public string Title { get; set; }

        public string OriginalScript { get; set; }

        public string ScriptType { get; set; } = "V4.00+";

        public string Collisions { get; set; } = "Normal";

        public int PlayResX { get; set; }

        public int PlayResY { get; set; }

        public static AssScriptInfoSection Parse(IEnumerable<string> lines) {
            var section = new AssScriptInfoSection();
            foreach (var line in lines) {
                var separatorIndex = line.IndexOf(AssLine.Separator);
                if (separatorIndex >= 0) {
                    switch (line.Substring(0, separatorIndex)) {
                        case "PlayResX":
                            section.PlayResX = int.Parse(line.Substring(separatorIndex + 1).Trim());
                            break;
                        case "PlayResY":
                            section.PlayResY = int.Parse(line.Substring(separatorIndex + 1).Trim());
                            break;
                    }
                }
            }

            return section;
        }

        public override IEnumerable<AssLine> AssLines {
            get {
                if (!string.IsNullOrEmpty(Title)) {
                    yield return new AssLine("Title", new List<string> {Title});
                }

                if (!string.IsNullOrEmpty(OriginalScript)) {
                    yield return new AssLine("Original Script", new List<string> {OriginalScript});
                }

                if (!string.IsNullOrEmpty(ScriptType)) {
                    yield return new AssLine("Script Type", new List<string> {ScriptType});
                }

                if (!string.IsNullOrEmpty(Collisions)) {
                    yield return new AssLine("Collisions", new List<string> {Collisions});
                }

                if (PlayResX > 0) {
                    yield return new AssLine("PlayResX", new List<string> {PlayResX.ToString()});
                }

                if (PlayResY > 0) {
                    yield return new AssLine("PlayResY", new List<string> {PlayResY.ToString()});
                }
            }
        }
    }
}
