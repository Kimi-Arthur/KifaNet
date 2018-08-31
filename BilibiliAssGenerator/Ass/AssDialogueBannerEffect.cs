using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssDialogueBannerEffect : AssDialogueEffect
    {
        public enum LeftToRightType
        {
            RightToLeft = 0,
            LeftToRight = 1
        }

        public override string EffectType => "Banner";

        public override IEnumerable<string> EffectParameters
            => new List<string>
            {
                Delay.GenerateAssText(),
                LeftToRight.GenerateAssText(),
                FadeAwayWidth.GenerateAssText()
            };

        int delay = 0;
        public int Delay
        {
            get
            {
                return delay;
            }
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(Delay));
                delay = value;
            }
        }

        public LeftToRightType LeftToRight { get; set; } = LeftToRightType.RightToLeft;

        public int FadeAwayWidth { get; set; } = 0;
    }
}
