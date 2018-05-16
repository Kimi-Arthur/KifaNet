using System;
using System.Diagnostics;
using CommandLine;

namespace Pimix.Apps.JobUtil {
    [Verb("run", HelpText = "Run a specific job.")]
    class RunJobCommand : JobUtilCommand {
        [Value(0)]
        public string JobId { get; set; }

        public override int Execute() {
            var runnerName = $"{ClientName}${Process.GetCurrentProcess().Id}";
            return Job.PullJob(JobId, ClientName + "-", runnerName).Execute(ClientName,
                FireHeartbeat ? HeartbeatInterval as TimeSpan? : null);
        }
    }
}
