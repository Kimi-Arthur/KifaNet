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

        [Option('t', "thread-count", HelpText = "Maximum thread count when running jobs.")]
        public int ThreadCount { get; set; } = 1;

        public override int Execute()
        {
            while (true)
            {
                Parallel.ForEach(
                    GetJobs(),
                    new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
                    j =>
                    {
                        var c = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Error.WriteLine($"Started job ({j.Id}) at {DateTime.Now}.");
                        Console.ForegroundColor = c;
                        j.Execute();
                        c = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"Finished job ({j.Id}) at {DateTime.Now}.");
                        Console.ForegroundColor = c;
                    });
                Console.Error.WriteLine("No jobs now. Sleep 2 minutes");
                Thread.Sleep(TimeSpan.FromMinutes(2));
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
                    var c = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Error.WriteLine($"Pulled job ({j.Id}) at {DateTime.Now}.");
                    Console.ForegroundColor = c;
                }
                catch (Exception ex)
                {
                    yield break;
                }

                if (j != null)
                    yield return j;
            }
        }
    }
}
