using System.Collections.Generic;

namespace Kifa.Tools.SubUtil;

public static class Common {
    public static HashSet<string> SubtitleExtensions { get; set; } = new() {
        "ass",
        "srt",
        "sup",
        "xml"
    };
}
