namespace Kifa.Markdown.Elements;

public class LinkElement : MarkdownElement {
    public string Text { get; set; }
    public string Target { get; set; }

    public override string ToText() => $"[{Text}]({Target})";
}
