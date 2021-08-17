using System.Collections.Generic;
using System.Linq;

namespace Kifa.Markdown.Elements {
    public class ParagraphElement : MarkdownElement {
        public List<MarkdownElement> ChildElements { get; set; }

        public override string ToText() =>
            $"{string.Join("", ChildElements.Select(element => element.ToText()))}\n\n";
    }
}
