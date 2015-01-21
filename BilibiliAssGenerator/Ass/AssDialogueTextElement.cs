using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssDialogueTextElement : AssElement
    {
        public string Content { get; set; }

        public bool? Bold { get; set; }

        public bool? Italic { get; set; }

        public bool? StrikeOut { get; set; }

        public int? Border { get; set; }

        public int? Shadow { get; set; }

        public bool? BlurEdges { get; set; }

        public string FontName { get; set; }

        public int FontSize { get; set; }

        public int? FontSizePercentX { get; set; }

        public int? FontSizePercentY { get; set; }

        public int? FontSpace { get; set; }

        public int? FontRotationX { get; set; }

        public int? FontRotationY { get; set; }

        public int? FontRotationZ { get; set; }

        public Color? TextColor { get; set; }

        public AssDialogueTextElement()
        {
            Content = "";
        }

        public AssDialogueTextElement(string s)
        {
            Content = s;
        }

        public static implicit operator AssDialogueTextElement(string s)
            => new AssDialogueTextElement(s);

        public override string GenerateAssText()
        {
            throw new NotImplementedException();
        }
    }
}