using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public abstract class AssDialogueEffect : AssElement {
        public abstract string EffectType { get; }
        public abstract IEnumerable<string> EffectParameters { get; }

        public override string GenerateAssText()
            => $"{EffectType};{string.Join(";", EffectParameters)}";
    }
}
