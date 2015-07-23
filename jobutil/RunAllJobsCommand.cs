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
                catch (Exception)
                {
                    Thread.Sleep(TimeSpan.FromHours(2));
                }

                if (j != null)
                {
                    new RunJobCommand { JobId = j.Id }.Execute();
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
