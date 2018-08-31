using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Ass
{
    public abstract class AssDialogueEffect : AssElement
    {
        public abstract string EffectType { get; }
        public abstract IEnumerable<string> EffectParameters { get; }

        public override string GenerateAssText()
            => $"{EffectType};{string.Join(";", EffectParameters)}";
    }
}
