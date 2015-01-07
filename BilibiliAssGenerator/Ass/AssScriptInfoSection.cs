using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliBiliAssGenerator.Ass
{
    public class AssScriptInfoSection : AssSection
    {
        public string Title { get; set; }
        public string OriginalScript { get; set; }
        public string ScriptType { get; set; } = "V4.00+";
        public override IEnumerable<AssLine> AssLines
        {
            get
            {
                if (!string.IsNullOrEmpty(Title))
                {
                    yield return new AssKeyValueLine("Title", Title);
                }

                if (!string.IsNullOrEmpty(OriginalScript))
                {
                    yield return new AssKeyValueLine("Original Script", OriginalScript);
                }

                if (!string.IsNullOrEmpty(ScriptType))
                {
                    yield return new AssKeyValueLine("Script Type", ScriptType);
                }
            }
        }
    }
}
