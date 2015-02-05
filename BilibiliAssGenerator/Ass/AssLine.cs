using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssLine : AssElement
    {
        public virtual string Key { get; protected set; }
        public virtual IEnumerable<string> Values { get; protected set; }

        public AssLine()
        {
        }

        public AssLine(string key, IEnumerable<string> values)
        {
            Key = key;
            Values = values;
        }

        public override string GenerateAssText()
            => $"{Key}: {string.Join(",", Values.Select(v => FormatValue(v)))}";

        string FormatValue(string value)
            => new Regex("[\r\n]+").Replace(value, @"\n");
    }
}
