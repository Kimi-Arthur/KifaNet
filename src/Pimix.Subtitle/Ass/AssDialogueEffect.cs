using System.Collections.Generic;

namespace Pimix.Subtitle.Ass {
    public class AssDialogueEffect : AssElement {
        public virtual string EffectType => null;
        public virtual IEnumerable<string> EffectParameters => null;

        public override string GenerateAssText()
            => EffectType == null ? "" : $"{EffectType};{string.Join(";", EffectParameters)}";
    }
}
