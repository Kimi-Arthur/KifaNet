using System.Collections.Generic;
using System.Linq;

namespace Kifa.Markdown.Elements {
    public class HeadingElement : MarkdownElement {
        public int Level { get; set; }
        public List<MarkdownElement> TitleElements { get; set; }


        public override string ToText() =>
            $"{new string('#', Level)} {TitleElements.Select(title => title.ToText()).JoinBy("")}\n\n";
    }
}
