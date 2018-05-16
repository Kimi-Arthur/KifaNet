using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Pimix.Apps.JobUtil {
    [Verb("all", HelpText = "Run all jobs continuously.")]
    class RunAllJobsCommand : JobUtilCommand {
        [Option('t', "thread-count", HelpText = "Maximum thread count when running jobs.")]
        public int ThreadCount { get; set; } = 1;

        public override int Execute() {
            var runnerName = $"{ClientName}${Process.GetCurrentProcess().Id}";
            while (true) {
                Parallel.ForEach(
                    GetJobs(runnerName),
                    new ParallelOptions {MaxDegreeOfParallelism = ThreadCount},
                    j => {
                        var c = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Error.WriteLine(
                            $"{runnerName}: Started job ({j.Id}) at {DateTime.Now}.");
                        Console.ForegroundColor = c;
                        j.Execute(ClientName,
                            FireHeartbeat ? HeartbeatInterval as TimeSpan? : null);
                        c = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(
                            $"{runnerName}: Finished job ({j.Id}) at {DateTime.Now}.");
                        Console.ForegroundColor = c;
                    });
                Console.Error.WriteLine("No jobs now. Sleep 2 minutes");
                Thread.Sleep(TimeSpan.FromMinutes(2));
            }
        }

        IEnumerable<Job> GetJobs(string runnerName) {
            while (true) {
                Job j = null;
                try {
                    j = Job.PullJob(idPrefix: ClientName + "-", runner: runnerName);
                    var c = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Error.WriteLine(
                        $"{runnerName}: Pulled job ({j.Id}) at {DateTime.Now}.");
                    Console.ForegroundColor = c;
                } catch (Exception) {
                    yield break;
                }

                if (j != null)
                    yield return j;
            }
        }
    }
}
