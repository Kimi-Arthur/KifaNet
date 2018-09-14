using System;
using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public class AssDialogueText : AssElement {
        public AssAlignment? Alignment { get; set; }

        public List<AssDialogueTextElement> TextElements { get; set; }

        public AssDialogueText() {
        }

        public AssDialogueText(AssDialogueTextElement element) {
            TextElements = new List<AssDialogueTextElement> {element};
        }

        public override string GenerateAssText() {
            var stylePrefix = "";
            stylePrefix += GenerateAssTextForAttribute("an", Alignment);

            return (!string.IsNullOrEmpty(stylePrefix) ? $"{{{stylePrefix}}}" : "") +
                   string.Concat(TextElements.Select(x => x.GenerateAssText()));
        }

        static string GenerateAssTextForAttribute(string name, Enum value)
            => value != null ? $"\\{name}{value:d}" : "";
    }
}
