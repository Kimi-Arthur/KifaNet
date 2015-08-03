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
        {
            var job = Job.StartJob(JobId);
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = job.Command;
                proc.StartInfo.Arguments = string.Join(" ", job.Arguments);

                Console.Error.WriteLine(proc.StartInfo.FileName);
                Console.Error.WriteLine(proc.StartInfo.Arguments);

                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;

                proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Job.AppendInfo(JobId, new Dictionary<string, object> { ["stdout"] = e.Data + "\n" });
                    }
                });

                proc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Job.AppendInfo(JobId, new Dictionary<string, object> { ["stderr"] = e.Data + "\n" });
                    }
                });

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                Job.AddInfo(JobId, new Dictionary<string, object> { ["exit_code"] = proc.ExitCode });
                Job.FinishJob(JobId, proc.ExitCode != 0);
                // Even if the job fails, the runner is ok.
                return 0;
            }
        }
    }
}
