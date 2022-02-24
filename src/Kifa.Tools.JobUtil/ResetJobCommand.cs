using System.Collections.Generic;
using CommandLine;

namespace Kifa.Tools.JobUtil; 

[Verb("reset", HelpText = "Reset jobs.")]
class ResetJobCommand : JobUtilCommand {
    [Value(0)]
    public IEnumerable<string> Jobs { get; set; }

    public override int Execute() {
        foreach (var job in Jobs) {
            Job.Client.ResetJob(job);
        }

        return 0;
    }
}