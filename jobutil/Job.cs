using System;
using System.Collections.Generic;
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

        public static Job StartJob(string id)
        {
            return PimixService.Call<Job, Job>("start_job", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id });
        }

        public static Job FinishJob(string id, bool failed = false)
        {
            return PimixService.Call<Job, Job>("finish_job", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id, ["failed"] = failed.ToString() });
        }

        public static Job AddInfo(string id, Dictionary<string, object> information)
        {
            return PimixService.Call<Job, Job>("add_info", methodType: "POST", parameters: new Dictionary<string, string> {["id"] = id }, body: information);
        }
    }
}
