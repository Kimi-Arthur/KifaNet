using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Ass
{
    public class AssEvent : AssLine
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
        
        public override IEnumerable<string> Values
            => new List<string>
            {
                Layer.GenerateAssText(),
                Start.GenerateAssText(),
                End.GenerateAssText(),
                Style.ValidName.GenerateAssText(),
                Name.GenerateAssText(),
                MarginL.HasValue ? $"{MarginL : D4}" : "0000",
                MarginR.HasValue ? $"{MarginR : D4}" : "0000",
                MarginV.HasValue ? $"{MarginV : D4}" : "0000",
                Effect.GenerateAssText(),
                Text.GenerateAssText()
            };
    }
}
