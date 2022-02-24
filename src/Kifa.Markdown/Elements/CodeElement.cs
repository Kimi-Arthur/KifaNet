namespace Kifa.Markdown.Elements; 

public class CodeElement : MarkdownElement {
    // Empty means no defined language.
    public string Language { get; set; }
    public string Code { get; set; }

    public override string ToText() => $"```{Language}\n{Code}\n```\n\n";
}