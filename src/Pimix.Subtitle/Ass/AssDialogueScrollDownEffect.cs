using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    class AssDialogueScrollDownEffect : AssDialogueEffect {
        public override IEnumerable<string> EffectParameters
            => new List<string> {
                AssElementExtensions.ToString(Y1),
                AssElementExtensions.ToString(Y2),
                AssElementExtensions.ToString(Delay),
                AssElementExtensions.ToString(FadeAwayHeight)
            };

        public override string EffectType => "Scroll down";

        public int Y1 { get; set; }

        public int Y2 { get; set; }

        public int Delay { get; set; }

        public int FadeAwayHeight { get; set; }
    }
}
