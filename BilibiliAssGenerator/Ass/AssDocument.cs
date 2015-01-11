using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGenerator.Ass
{
    public class AssDocument : AssElement
    {
        public AssSection Sections { get; set; }
        public override string GenerateAssText()
        {
            throw new NotImplementedException();
        }
    }
}
