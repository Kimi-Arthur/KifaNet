using System.Collections.Generic;

namespace Pimix.Apps.NoteUtil {
    public class WordsSection {
        public string Type { get; set; }
        public List<string> ColumnNames { get; set; }
        public Dictionary<string, List<string>> Lines { get; set; }
    }
}
