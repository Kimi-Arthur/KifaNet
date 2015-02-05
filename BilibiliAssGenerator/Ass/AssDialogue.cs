using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssDialogue : AssLine
    {
        public int Layer { get; set; } = 0;

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public AssStyle Style { get; set; } = AssStyle.DefaultStyle;

        public string Name { get; set; } = "NTP";

        public int? MarginL { get; set; }

        public int? MarginR { get; set; }

        public int? MarginV { get; set; }

        public AssDialogueEffect Effect { get; set; }

        public AssDialogueText Text { get; set; }

        public override string Key => "Dialogue";

        public override IEnumerable<string> Values
            => new List<string>
            {
                Layer.GenerateAssText(),
                Start.GenerateAssText(),
                End.GenerateAssText(),
                Style.ValidName.GenerateAssText(),
                MarginL.HasValue ? "0000" : $"{MarginL : D4}",
                MarginR.HasValue ? "0000" : $"{MarginR : D4}",
                MarginV.HasValue ? "0000" : $"{MarginV : D4}",
                Effect.GenerateAssText(),
                Text.GenerateAssText()
            };
    }
}
