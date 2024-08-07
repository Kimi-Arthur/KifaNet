﻿using System.Collections.Generic;

namespace Kifa.Subtitle.Ass;

class AssDialogueScrollDownEffect : AssDialogueEffect {
    public override IEnumerable<string> EffectParameters
        => new List<string> {
            Y1.ToString(),
            Y2.ToString(),
            Delay.ToString(),
            FadeAwayHeight.ToString()
        };

    public override string EffectType => "Scroll down";

    public int Y1 { get; set; }

    public int Y2 { get; set; }

    public int Delay { get; set; }

    public int FadeAwayHeight { get; set; }
}
