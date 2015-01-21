using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssDialogueText : AssElement
    {
        public AssAlignment Alignment { get; set; }

        public List<AssDialogueTextElement> TextElements { get; set; }

        public override string GenerateAssText()
            => string.Join("", TextElements.Select(x => x.GenerateAssText()));
    }
}
