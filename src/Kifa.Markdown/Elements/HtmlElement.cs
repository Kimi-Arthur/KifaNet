namespace Kifa.Markdown.Elements; 

public class HtmlElement : MarkdownElement {
    public string Html { get; set; }

    public override string ToText() => Html.Replace("\n", " ");
}