using System.Collections.Generic;
using System.Linq;

namespace Kifa.Markdown.Elements {
    public class UnorderedListItemElement : MarkdownElement {
        // Level starting from 0.
        public int Level { get; set; }

        public List<MarkdownElement> ChildElements { get; set; }

        public override string ToText() =>
            $"{new string(' ', Level * 2)}- {ChildElements.TakeWhile(element => element is not UnorderedListItemElement).Select(element => element.ToText()).JoinBy().TrimEnd()}\n" +
            ChildElements.SkipWhile(element => element is not UnorderedListItemElement)
                .Select(element => element.ToText()).JoinBy();
    }
}
