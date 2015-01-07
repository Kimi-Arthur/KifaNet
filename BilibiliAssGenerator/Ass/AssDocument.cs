using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliBiliAssGenerator.Ass
{
    public class AssDocument : AssElement
    {
        public AssSection Sections { get; set; }
        public override string GenerateText()
        {
            throw new NotImplementedException();
        }
    }
}
