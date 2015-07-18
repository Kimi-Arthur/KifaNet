using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace jobutil
{
    [Verb("run", HelpText = "Run a specific job.")]
    class RunJobCommand : Command
    {
        [Value(0, Required = true)]
        public string JobId { get; set; }

        public override int Execute()
        {
            var job = Job.StartJob(JobId);
            Process proc = new Process();
            proc.StartInfo.FileName = job.Command;
            proc.StartInfo.Arguments = string.Join(" ", job.Arguments);
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.WaitForExit();
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            Job.AddInfo(JobId, new Dictionary<string, object> { ["stdout"] = stdout, ["stderr"] = stderr, ["exit_code"] = proc.ExitCode });
            Job.FinishJob(JobId, proc.ExitCode != 0);
            // Even if the job fails, the runner is ok.
            return 0;
        }
    }
}
