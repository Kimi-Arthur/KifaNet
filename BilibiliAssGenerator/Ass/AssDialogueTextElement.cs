using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public abstract class AssDialogueTextElement : AssElement
    {
        public static implicit operator AssDialogueTextElement(string s)
            => new AssDialogueTextNormalElement(s);
    }
}