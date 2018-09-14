using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public class AssScriptInfoSection : AssSection {
        public override string SectionTitle { get; } = "Script Info";

        public string Title { get; set; }

        public string OriginalScript { get; set; }

        public string ScriptType { get; set; } = "V4.00+";

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
            }
        }
    }
}
