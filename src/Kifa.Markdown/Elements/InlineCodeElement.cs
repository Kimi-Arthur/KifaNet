namespace Kifa.Markdown.Elements {
    public class InlineCodeElement : MarkdownElement {
        public string Code { get; set; }

        public override string ToText() => $"`{Code}`";
    }
}
