
namespace Pimix.Subtitle.Ass {
    public class AssDialogueControlTextElement : AssDialogueTextElement {
        public string Content { get; set; }

        public override string ToString() => Content;

        public new static AssDialogueTextElement Parse(string content)
            => new AssDialogueControlTextElement {Content = content};
    }
}
