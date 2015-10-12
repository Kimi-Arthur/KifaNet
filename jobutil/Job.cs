using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace jobutil
{
    [DataModel("jobs")]
    class Job
    {
        [JsonProperty("$id")]
        public string Id { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("arguments")]
        public List<string> Arguments { get; set; }

        public int Execute(string runnerName = null)
        {
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
                        Job.AppendInfo(Id, new Dictionary<string, object> {["stdout"] = e.Data + "\n" });
                    }
                });

                proc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Job.AppendInfo(Id, new Dictionary<string, object> {["stderr"] = e.Data + "\n" });
                    }
                });

                proc.Start();

                runnerName = $"{runnerName}${proc.Id}";

                Job.StartJob(Id, runner: runnerName);
                Console.Error.WriteLine($"{runnerName}: Job start info ({Id}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                Job.AddInfo(Id, new Dictionary<string, object> {["exit_code"] = proc.ExitCode });
                Job.FinishJob(Id, proc.ExitCode != 0);

                Console.Error.WriteLine($"{runnerName}: Job finish info ({Id}): {proc.ExitCode}");

                return proc.ExitCode;
            }
        }

        #region PimixService Wrappers

        public static string PimixServerApiAddress
        {
            get
            {
                return PimixService.PimixServerApiAddress;
            }
            set
            {
                PimixService.PimixServerApiAddress = value;
            }
        }

        public static string PimixServerCredential
        {
            get
            {
                return PimixService.PimixServerCredential;
            }
            set
            {
                PimixService.PimixServerCredential = value;
            }
        }

        public static bool Patch(Job data, string id = null)
            => PimixService.Patch<Job>(data, id);

        public static Job Get(string id)
            => PimixService.Get<Job>(id);

        #endregion

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
        {
            PimixService.Call<Job>
            (
                "reset_job",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id
                }
            );
        }

        public static void FinishJob(string id, bool failed = false)
        {
            PimixService.Call<Job>
            (
                "finish_job",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["failed"] = failed.ToString()
                }
            );
        }

        public static void Heartbeat(string id)
        {
            PimixService.Call<Job>
            (
                "heartbeat",
                methodType: "POST",
                parameters: new Dictionary<string, string>
                {
                    ["id"] = id
                }
            );
        }

        public static void AddInfo(string id, Dictionary<string, object> information)
        {
            PimixService.Call<Job>("add_info", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id }, body: information);
        }

        public static void AppendInfo(string id, Dictionary<string, object> information)
        {
            PimixService.Call<Job>("append_info", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id }, body: information);
        }

        public static Job GetJob(string idPrefix = null)
        {
            return PimixService.Call<Job, Job>("get_job", parameters: new Dictionary<string, string> {["id_prefix"] = idPrefix });
        }
    }
}
