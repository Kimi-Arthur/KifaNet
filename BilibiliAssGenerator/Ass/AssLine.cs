using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssLine : AssElement
    {
        public virtual string Key { get; set; }
        public virtual IEnumerable<string> Values { get; set; }

        public AssLine()
        {
        }

        public AssLine(string key, IEnumerable<string> values)
        {
            Key = key;
            Values = values;
        }

        public override string GenerateText()
            => "\{Key}: \{string.Join(",", Values)}";
    }
}
