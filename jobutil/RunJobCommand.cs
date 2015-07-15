using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace jobutil
{
    class RunJobCommand : Command
    {
        [Value(0, Required = true)]
        public string JobId { get; set; }

        public override int Execute()
        {
            return -1;
        }
    }
}
