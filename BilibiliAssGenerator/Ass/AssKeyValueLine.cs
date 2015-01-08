using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssKeyValueLine : AssLine
    {
        public AssKeyValueLine(string key, string value)
        {
            Key = key;
            Values = new List<string>() { value };
        }
    }
}
