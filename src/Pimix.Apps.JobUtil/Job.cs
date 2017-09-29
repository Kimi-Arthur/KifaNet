using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Apps.JobUtil
{
    [DataModel("jobs")]
    partial class Job
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("arguments")]
        public List<string> Arguments { get; set; }

        public int Execute(string runnerName = null, TimeSpan? heartbeatInterval = null)
        {
            Timer timer = null;
            if (heartbeatInterval != null)
            {
                timer = new Timer(heartbeatInterval.Value.TotalMilliseconds);
                timer.Elapsed += new ElapsedEventHandler((sender, e) =>
                {
                    Job.Heartbeat(Id);
                });
            }

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = Command;
                proc.StartInfo.Arguments = string.Join(" ", Arguments);

                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;

                proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        try
                        {
                            Job.Log(Id, e.Data, "i");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Exception during uploading log:\n{ex}.");
                        }

                        timer.Interval = timer.Interval;
                    }
                });

                proc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        try
                        {
                            Job.Log(Id, e.Data, "d");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Exception during uploading log:\n{ex}.");
                        }

                        timer.Interval = timer.Interval;
                    }
                });

                timer?.Start();
                proc.Start();

                runnerName = $"{runnerName}${proc.Id}";

                Job.StartJob(Id, runner: runnerName);
                Console.Error.WriteLine($"{runnerName}: Job start info ({Id}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                Job.FinishJob(Id, proc.ExitCode);

                timer?.Dispose();

                Console.Error.WriteLine($"{runnerName}: Job finish info ({Id}): {proc.ExitCode}");

                return proc.ExitCode;
            }
        }

        public static Job PullJob(string id = null, string idPrefix = null, string runner = null)
            => PimixService.Call<Job, Job>
            (
                "pull_job",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                }
            );

        public static Job StartJob(string id = null, string idPrefix = null, string runner = null)
            => PimixService.Call<Job, Job>
            (
                "start_job",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                }
            );

        public static void ResetJob(string id)
            => PimixService.Call<Job>
            (
                "reset_job",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id
                }
            );

        public static void FinishJob(string id, bool failed = false)
            => PimixService.Call<Job>
            (
                "finish_job",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["failed"] = failed.ToString()
                }
            );

        public static void Heartbeat(string id)
            => PimixService.Call<Job>
            (
                "heartbeat",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id
                }
            );

        public static void FinishJob(string id, int exit_code = 0)
            => PimixService.Call<Job>("finish_job", methodType: "POST", body: new Dictionary<string, object> { ["id"] = id, ["exit_code"] = exit_code });

        public static void Log(string id, string message, string level = "i")
            => PimixService.Call<Job>("log", methodType: "POST", body: new Dictionary<string, string> { ["id"] = id, ["level"] = level,  ["message"] = message});

        public static Job GetJob(string idPrefix = null)
            => PimixService.Call<Job, Job>("get_job", parameters: new Dictionary<string, string> { ["id_prefix"] = idPrefix });
    }
}
