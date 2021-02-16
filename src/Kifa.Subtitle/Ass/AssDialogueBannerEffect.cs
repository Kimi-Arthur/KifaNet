using System;
using System.Collections.Generic;

namespace Kifa.Subtitle.Ass {
    public class AssDialogueBannerEffect : AssDialogueEffect {
        public enum LeftToRightType {
            RightToLeft = 0,
            LeftToRight = 1
        }

        public const string EffectTypeName = "Banner";
        public override string EffectType => EffectTypeName;

        public override IEnumerable<string> EffectParameters
            => new List<string> {
                Delay.ToString(),
                $"{LeftToRight:d}",
                FadeAwayWidth.ToString()
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
