using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    [DataModel("jobs")]
    class Job {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("arguments")]
        public List<string> Arguments { get; set; }

        public int Execute(string runnerName = null, TimeSpan? heartbeatInterval = null) {
            Timer timer = null;
            if (heartbeatInterval != null) {
                timer = new Timer(heartbeatInterval.Value.TotalMilliseconds);
                timer.Elapsed += (sender, e) => { Heartbeat(Id); };
            }

            using (var proc = new Process()) {
                proc.StartInfo.FileName = Command;
                proc.StartInfo.Arguments = string.Join(" ", Arguments);

                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;

                proc.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        try {
                            Log(Id, e.Data, "info");
                        } catch (Exception ex) {
                            Console.Error.WriteLine($"Exception during uploading log:\n{ex}.");
                        }

                        timer.Interval = timer.Interval;
                    }
                };

                proc.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        try {
                            Log(Id, e.Data, "debug");
                        } catch (Exception ex) {
                            Console.Error.WriteLine($"Exception during uploading log:\n{ex}.");
                        }

                        timer.Interval = timer.Interval;
                    }
                };

                timer?.Start();
                proc.Start();

                runnerName = $"{runnerName}${proc.Id}";

                StartJob(Id, runner: runnerName);
                Console.Error.WriteLine(
                    $"{runnerName}: Job start info ({Id}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                FinishJob(Id, proc.ExitCode);

                timer?.Dispose();

                Console.Error.WriteLine($"{runnerName}: Job finish info ({Id}): {proc.ExitCode}");

                return proc.ExitCode;
            }
        }

        public static Job PullJob(string id = null, string idPrefix = null, string runner = null)
            => PimixService.Call<Job, Job>
            (
                "pull_job",
                id,
                new Dictionary<string, object> {
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                }
            );

        public static Job StartJob(string id = null, string idPrefix = null, string runner = null)
            => PimixService.Call<Job, Job>
            (
                "start_job",
                id,
                new Dictionary<string, object> {
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                }
            );

        public static void ResetJob(string id)
            => PimixService.Call<Job>
            (
                "reset_job",
                id
            );

        public static void Heartbeat(string id)
            => PimixService.Call<Job>
            (
                "heartbeat",
                id
            );

        public static void FinishJob(string id, int exitCode = 0)
            => PimixService.Call<Job>("finish_job", id,
                new Dictionary<string, object> {["exit_code"] = exitCode});

        public static void Log(string id, string message, string level = "i")
            => PimixService.Call<Job>("log", id,
                new Dictionary<string, object> {
                    ["level"] = level,
                    ["message"] = message
                });
    }
}
