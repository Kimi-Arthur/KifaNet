
namespace Pimix.Subtitle.Ass {
    public class AssDialogueTextControlElement : AssDialogueTextElement {
        public string Content { get; set; }

        public override string ToString() => Content;

        public new static AssDialogueTextElement Parse(string content)
            => new AssDialogueTextControlElement {Content = content};
    }
}
