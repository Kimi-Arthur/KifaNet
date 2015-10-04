using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Service
{
    public class ActionFailedException : Exception
    {
        public ActionStatus Response { get; set; }

        public override string ToString()
            => $"Action failed with \"{Response}\".";
    }
}
