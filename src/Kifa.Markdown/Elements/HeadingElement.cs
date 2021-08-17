namespace Kifa.Markdown.Elements {
    public class HeadingElement : MarkdownElement {
        public int Level { get; set; }
        public string Title { get; set; }


        public override string ToText() => $"{new string('#', Level)} {Title}\n\n";
    }
}
