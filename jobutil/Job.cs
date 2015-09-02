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

        public int Execute()
        {
            using (Process proc = new Process())
            {
                Job.StartJob(Id);
                proc.StartInfo.FileName = Command;
                proc.StartInfo.Arguments = string.Join(" ", Arguments);

                Console.Error.WriteLine($"Job start info ({Id}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

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
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.WaitForExit();
                Job.AddInfo(Id, new Dictionary<string, object> {["exit_code"] = proc.ExitCode });
                Job.FinishJob(Id, proc.ExitCode != 0);

                Console.Error.WriteLine($"Job finish info ({Id}): {proc.ExitCode}");

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

        public static Job PullJob(string id = null, string idPrefix = null)
        {
            return PimixService.Call<Job, Job>("pull_job", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id, ["id_prefix"] = idPrefix });
        }

        public static Job StartJob(string id = null, string idPrefix = null)
        {
            return PimixService.Call<Job, Job>("start_job", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id, ["id_prefix"] = idPrefix });
        }

        public static Job FinishJob(string id, bool failed = false)
        {
            return PimixService.Call<Job, Job>("finish_job", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id,["failed"] = failed.ToString() });
        }

        public static Job AddInfo(string id, Dictionary<string, object> information)
        {
            return PimixService.Call<Job, Job>("add_info", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id }, body: information);
        }

        public static Job AppendInfo(string id, Dictionary<string, object> information)
        {
            return PimixService.Call<Job, Job>("append_info", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id }, body: information);
        }

        public static Job GetJob(string idPrefix = null)
        {
            return PimixService.Call<Job, Job>("get_job", parameters: new Dictionary<string, string> {["id_prefix"] = idPrefix });
        }
    }
}
