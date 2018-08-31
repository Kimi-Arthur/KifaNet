using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Ass
{
    class AssDialogueScrollDownEffect : AssDialogueEffect
    {
        public override IEnumerable<string> EffectParameters
            => new List<string>
            {
                Y1.GenerateAssText(),
                Y2.GenerateAssText(),
                Delay.GenerateAssText(),
                FadeAwayHeight.GenerateAssText()
            };

        public override string EffectType => "Scroll down";

        public int Y1 { get; set; }

        public int Y2 { get; set; }

        public int Delay { get; set; }

        public int FadeAwayHeight { get; set; }
    }
}
