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
        [Value(0)]
        public string JobPrefix { get; set; }

        public override int Execute()
        {
            while (true)
            {
                try
                {
                    var j = Job.GetJob(JobPrefix);

                    if (!string.IsNullOrEmpty(j?.Id))
                    {
                        Job.StartJob(j.Id).Execute();
                        Console.Error.WriteLine("Finished one job. Sleep 5 seconds");
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                    else
                    {
                        Console.Error.WriteLine("Unexpected job object.");
                        Thread.Sleep(TimeSpan.FromMinutes(2));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("No jobs now. Sleep 2 hours");
                    Thread.Sleep(TimeSpan.FromMinutes(2));
                }
            }
        }
    }
}
