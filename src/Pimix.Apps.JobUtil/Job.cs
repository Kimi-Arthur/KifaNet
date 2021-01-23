using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    class Job : DataModel {
        public const string ModelId = "jobs";

        static JobServiceClient client;

        public static JobServiceClient Client => client ??= new JobRestServiceClient();

        public string Command { get; set; }

        public List<string> Arguments { get; set; }

        public int Execute(string runnerName = null, TimeSpan? heartbeatInterval = null) {
            Timer timer = null;
            if (heartbeatInterval != null) {
                timer = new Timer(heartbeatInterval.Value.TotalMilliseconds);
                timer.Elapsed += (sender, e) => { Client.Heartbeat(Id); };
            }

            using var proc = new Process();
            proc.StartInfo.FileName = Command;
            proc.StartInfo.Arguments = string.Join(" ", Arguments);

            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;

            proc.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    try {
                        Client.Log(Id, e.Data, "info");
                    } catch (Exception ex) {
                        Console.Error.WriteLine($"Exception during uploading log:\n{ex}.");
                    }

                    timer.Interval = timer.Interval;
                }
            };

            proc.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    try {
                        Client.Log(Id, e.Data, "debug");
                    } catch (Exception ex) {
                        Console.Error.WriteLine($"Exception during uploading log:\n{ex}.");
                    }

                    timer.Interval = timer.Interval;
                }
            };

            timer?.Start();
            proc.Start();

            runnerName = $"{runnerName}${proc.Id}";

            Client.StartJob(Id, runner: runnerName);
            Console.Error.WriteLine(
                $"{runnerName}: Job start info ({Id}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();
            Client.FinishJob(Id, proc.ExitCode);

            timer?.Dispose();

            Console.Error.WriteLine($"{runnerName}: Job finish info ({Id}): {proc.ExitCode}");

            return proc.ExitCode;
        }
    }

    interface JobServiceClient : KifaServiceClient<Job> {
        Job PullJob(string id = null, string idPrefix = null, string runner = null);
        Job StartJob(string id = null, string idPrefix = null, string runner = null);
        void ResetJob(string id);
        void Heartbeat(string id);
        void FinishJob(string id, int exitCode = 0);
        void Log(string id, string message, string level = "i");
    }

    class JobRestServiceClient : KifaServiceRestClient<Job>, JobServiceClient {
        public Job PullJob(string id = null, string idPrefix = null, string runner = null)
            => Call<Job>("pull_job",
                id,
                new Dictionary<string, object> {
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                });

        public Job StartJob(string id = null, string idPrefix = null, string runner = null)
            => Call<Job>("start_job",
                id,
                new Dictionary<string, object> {
                    ["id_prefix"] = idPrefix,
                    ["runner"] = runner
                });

        public void ResetJob(string id)
            => Call("reset_job",
                id);

        public void Heartbeat(string id)
            => Call("heartbeat",
                id);

        public void FinishJob(string id, int exitCode = 0)
            => Call("finish_job", id,
                new Dictionary<string, object> {
                    ["exit_code"] = exitCode
                });

        public void Log(string id, string message, string level = "i")
            => Call("log", id,
                new Dictionary<string, object> {
                    ["level"] = level,
                    ["message"] = message
                });
    }
}
