namespace Kifa.Subtitle.Ass; 

public abstract class AssDialogueTextElement {
    public static AssDialogueTextElement Parse(string content)
        => content.StartsWith('{')
            ? AssDialogueControlTextElement.Parse(content)
            : AssDialogueRawTextElement.Parse(content);
}