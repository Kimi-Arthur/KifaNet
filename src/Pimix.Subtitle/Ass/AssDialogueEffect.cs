using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public class AssDialogueEffect {
        public virtual string EffectType => null;
        public virtual IEnumerable<string> EffectParameters => null;

        public override string ToString()
            => EffectType == null ? "" : $"{EffectType};{string.Join(";", EffectParameters)}";
    }
}
