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
            foreach (var j in GetJobs())
            {
                Console.Error.WriteLine($"Started job ({j.Id}) at {DateTime.Now}.");
                j.Execute();
                Console.Error.WriteLine($"Finished job ({j.Id}) at {DateTime.Now}.");
            }

            return 0;
        }

        IEnumerable<Job> GetJobs()
        {
            while (true)
            {
                Job j = null;
                try
                {
                    j = Job.StartJob();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("No jobs now. Sleep 2 minutes");
                    Thread.Sleep(TimeSpan.FromMinutes(2));
                }

                if (j != null)
                    yield return j;
            }
        }
    }
}
