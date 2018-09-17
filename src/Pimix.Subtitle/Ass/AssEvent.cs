using System;
using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public class AssEvent : AssLine {
        public int Layer { get; set; } = 0;

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
                Style.ValidName,
                Name,
                $"{MarginL:D4}",
                $"{MarginR:D4}",
                $"{MarginV:D4}",
                Effect.ToString(),
                Text.ToString()
            };
    }
}
