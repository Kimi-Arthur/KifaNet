using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using CommandLine;

namespace jobutil
{
    [Verb("run", HelpText = "Run a specific job.")]
    class RunJobCommand : Command
    {
        [Value(0, Required = true)]
        public string JobId { get; set; }

        public override int Execute()
            => Job.StartJob(JobId).Execute();
    }
}
