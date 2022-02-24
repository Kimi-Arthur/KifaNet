using System.Collections.Generic;
using System.Linq;

namespace Kifa.Markdown.Elements;

public class ParagraphElement : MarkdownElement {
    public List<MarkdownElement> ChildElements { get; set; }

    public override string ToText()
        => $"{ChildElements.Select(element => element.ToText()).JoinBy().Trim()}\n\n";
}
