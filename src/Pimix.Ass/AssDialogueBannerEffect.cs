using System;
using System.Collections.Generic;

namespace Pimix.Ass {
    public class AssDialogueBannerEffect : AssDialogueEffect {
        public enum LeftToRightType {
            RightToLeft = 0,
            LeftToRight = 1
        }

        public override string EffectType => "Banner";

        public override IEnumerable<string> EffectParameters
            => new List<string> {
                Delay.GenerateAssText(),
                LeftToRight.GenerateAssText(),
                FadeAwayWidth.GenerateAssText()
            };

        int delay;

        public int Delay {
            get => delay;
            set {
                if (value < 0 || value > 100) {
                    throw new ArgumentOutOfRangeException(nameof(Delay));
                }

                delay = value;
            }
        }

        public LeftToRightType LeftToRight { get; set; } = LeftToRightType.RightToLeft;

        public int FadeAwayWidth { get; set; } = 0;
    }
}
