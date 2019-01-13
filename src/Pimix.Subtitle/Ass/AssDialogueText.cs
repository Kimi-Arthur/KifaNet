using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pimix.Subtitle.Ass {
    public class AssDialogueText {
        static readonly Regex textElementPattern = new Regex("{[^}]*}|[^{]*");

        public AssAlignment? Alignment { get; set; }

        public List<AssDialogueTextElement> TextElements { get; set; } =
            new List<AssDialogueTextElement>();

        public override string ToString() {
            var stylePrefix = "";
            stylePrefix += GenerateAssTextForAttribute("an", Alignment);

            return (!string.IsNullOrEmpty(stylePrefix) ? $"{{{stylePrefix}}}" : "") +
                   string.Concat(TextElements.Select(x => x.ToString()));
        }

        static string GenerateAssTextForAttribute(string name, Enum value)
            => value != null ? $"\\{name}{value:d}" : "";

        public static AssDialogueText Parse(string content) {
            return new AssDialogueText {
                TextElements = textElementPattern.Matches(content)
                    .Select(m => AssDialogueTextElement.Parse(m.Value)).ToList()
            };
        }
    }
}
