using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    class AssDialogueTextNormalElement : AssElement
    {
        public string Content { get; set; }

        public AssDialogueTextNormalElement()
        {
            Content = "";
        }

        public AssDialogueTextNormalElement(string s)
        {
            Content = s;
        }

        public static implicit operator AssDialogueTextNormalElement(string s)
            => new AssDialogueTextNormalElement(s);

        public override string GenerateAssText()
            => Content;
    }
}
