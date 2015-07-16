using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string cmd = Job.StartJob(JobId).Command;
            Process proc = new Process();
            proc.StartInfo.FileName = cmd;
            proc.StartInfo.Arguments = cmd;
            proc.Start();
            proc.WaitForExit();
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            Job.AddInfo(JobId, new Dictionary<string, object> { ["stdout"] = stdout, ["stderr"] = stderr, ["exit_code"] = proc.ExitCode.ToString() });
            Job.FinishJob(JobId, proc.ExitCode != 0);
            // Even if the job fails, the runner is ok.
            return 0;
        }
    }
}
