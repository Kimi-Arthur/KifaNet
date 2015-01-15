using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    class AssDialogue : AssLine
    {
        public int Layer { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public AssStyle Style { get; set; }
        public string Name { get; set; } = "NTP";
        public int MarginL { get; set; }
        public int MarginR { get; set; }
        public int MarginV { get; set; }
        public AssDialoguoEffect Effect { get; set; }
        public AssDialogueText Text { get; set; }
        public override string Key => "Dialogue";
        public override IEnumerable<string> Values
            => new List<string>
            {
                Layer.GenerateAssText(),
                Start.GenerateAssText(),
                End.GenerateAssText(),
                Style.ValidName.GenerateAssText(),
                MarginL.GenerateAssText(),
                MarginR.GenerateAssText(),
                MarginV.GenerateAssText(),
                Effect.GenerateAssText(),
                Text.GenerateAssText()
            };
    }
}
