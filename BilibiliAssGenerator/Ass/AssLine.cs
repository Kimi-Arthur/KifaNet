using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliBiliAssGenerator.Ass
{
    public class AssLine : AssElement
    {
        public virtual string Key { get; set; }
        public virtual IEnumerable<string> Values { get; set; }
        public override string GenerateText()
            => "\{Key}: \{string.Join(",", Values)}";
    }
}
