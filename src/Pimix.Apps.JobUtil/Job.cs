using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    [DataModel("jobs")]
    partial class Job {
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
                "POST",
                parameters: new Dictionary<string, string> {
                    ["id"] = id,
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                }
            );

        public static Job StartJob(string id = null, string idPrefix = null, string runner = null)
            => PimixService.Call<Job, Job>
            (
                "start_job",
                "POST",
                parameters: new Dictionary<string, string> {
                    ["id"] = id,
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                }
            );

        public static void ResetJob(string id)
            => PimixService.Call<Job>
            (
                "reset_job",
                "POST",
                parameters: new Dictionary<string, string> {
                    ["id"] = id
                }
            );

        public static void FinishJob(string id, bool failed = false)
            => PimixService.Call<Job>
            (
                "finish_job",
                "POST",
                parameters: new Dictionary<string, string> {
                    ["id"] = id,
                    ["failed"] = failed.ToString()
                }
            );

        public static void Heartbeat(string id)
            => PimixService.Call<Job>
            (
                "heartbeat",
                "POST",
                parameters: new Dictionary<string, string> {
                    ["id"] = id
                }
            );

        public static void FinishJob(string id, int exit_code = 0)
            => PimixService.Call<Job>("finish_job", "POST",
                body: new Dictionary<string, object> {["id"] = id, ["exit_code"] = exit_code});

        public static void Log(string id, string message, string level = "i")
            => PimixService.Call<Job>("log", "POST",
                body: new Dictionary<string, string> {
                    ["id"] = id,
                    ["level"] = level,
                    ["message"] = message
                });

        public static Job GetJob(string idPrefix = null)
            => PimixService.Call<Job, Job>("get_job",
                parameters: new Dictionary<string, string> {["id_prefix"] = idPrefix});
    }
}
