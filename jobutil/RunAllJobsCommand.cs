using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace jobutil
{
    [Verb("all", HelpText = "Run all jobs continuously.")]
    class RunAllJobsCommand : Command
    {
        public override int Execute()
        {
            while (true)
            {
                Job j = null;
                try
                {
                    j = Job.GetJob();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("No jobs now. Sleep 2 hours");
                    Console.WriteLine(ex);
                    Thread.Sleep(TimeSpan.FromHours(2));
                }

                if (j != null)
                {
                    new RunJobCommand { JobId = j.Id }.Execute();
                    Console.Error.WriteLine("Finished one job. Sleep 5 seconds");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
