using System;
using System.Collections.Generic;
using System.Linq;

namespace Pimix.Subtitle.Ass {
    public class AssEvent : AssLine {
        public int Layer { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public AssStyle Style { get; set; } = AssStyle.DefaultStyle;

        public string Name { get; set; } = "";

        public int MarginL { get; set; }

        public int MarginR { get; set; }

        public int MarginV { get; set; }

        public AssDialogueEffect Effect { get; set; } = new AssDialogueEffect();

        public AssDialogueText Text { get; set; }

        public override IEnumerable<string> Values
            => new List<string> {
                Layer.ToString(),
                $"{Start:h\\:mm\\:ss\\.ff}",
                $"{End:h\\:mm\\:ss\\.ff}",
                Style.Name,
                Name,
                $"{MarginL:D4}",
                $"{MarginR:D4}",
                $"{MarginV:D4}",
                Effect.ToString(),
                Text.ToString()
            };

        public static AssEvent Parse(Dictionary<string, AssStyle> styles, string eventType, IEnumerable<string> content,
            IEnumerable<string> headers) {
            AssEvent assEvent = null;
            switch (eventType) {
                case AssDialogue.EventType:
                    assEvent = new AssDialogue();
                    break;
            }

            if (assEvent == null) {
                return null;
            }

            foreach (var p in content.Zip(headers, Tuple.Create)) {
                switch (p.Item2) {
                    case "Layer":
                        assEvent.Layer = int.Parse(p.Item1);
                        break;
                    case "Start":
                        assEvent.Start = TimeSpan.Parse(p.Item1);
                        break;
                    case "End":
                        assEvent.End = TimeSpan.Parse(p.Item1);
                        break;
                    case "Style":
                        assEvent.Style = styles[p.Item1];
                        break;
                    case "Actor":
                    case "Name":
                        assEvent.Name = p.Item1;
                        break;
                    case "MarginL":
                        assEvent.MarginL = int.Parse(p.Item1);
                        break;
                    case "MarginR":
                        assEvent.MarginR = int.Parse(p.Item1);
                        break;
                    case "MarginV":
                        assEvent.MarginV = int.Parse(p.Item1);
                        break;
                    case "Effect":
                        assEvent.Effect = AssDialogueEffect.Parse(p.Item1);
                        break;
                    case "Text":
                        assEvent.Text = AssDialogueText.Parse(p.Item1);
                        break;
                }
            }

            return assEvent;
        }
    }
}
