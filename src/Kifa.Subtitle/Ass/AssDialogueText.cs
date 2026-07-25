using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kifa.Subtitle.Ass;

public class AssDialogueText {
    static readonly Regex textElementPattern = new("{[^}]*}|[^{]*");

    public List<AssDialogueTextElement> TextElements { get; set; } = [];

    public override string ToString() => string.Concat(TextElements.Select(x => x.ToString()));

    static string GenerateAssTextForAttribute(string name, Enum? value)
        => string.FormatOrEmpty($"\\{name}{value:d}");

    public static AssDialogueText Parse(string content) {
        return new AssDialogueText {
            TextElements = textElementPattern.Matches(content)
                .Select(m => AssDialogueTextElement.Parse(m.Value)).ToList()
        };
    }
}
