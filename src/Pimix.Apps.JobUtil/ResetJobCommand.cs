using System.Collections.Generic;
using CommandLine;

namespace Pimix.Apps.JobUtil
{
    [Verb("reset", HelpText = "Reset jobs.")]
    class ResetJobCommand : JobUtilCommand
    {
        [Value(0)]
        public IEnumerable<string> Jobs { get; set; }

        public override int Execute()
        {
            foreach (var job in Jobs)
            {
                Job.ResetJob(job);
            }

            return 0;
        }
    }
}
