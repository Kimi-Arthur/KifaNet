namespace Kifa.Subtitle.Ass;

public class AssDialogueRawTextElement : AssDialogueTextElement {
    public string Content { get; set; }

    public override string ToString() => Content;

    public new static AssDialogueTextElement Parse(string content)
        => new AssDialogueRawTextElement {
            Content = content
        };
}
