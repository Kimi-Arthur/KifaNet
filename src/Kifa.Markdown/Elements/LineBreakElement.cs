namespace Kifa.Markdown.Elements; 

public class LineBreakElement : MarkdownElement {
    public override string ToText() => "\n";
}