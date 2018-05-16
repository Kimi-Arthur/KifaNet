using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    partial class Job {
        public static string PimixServerApiAddress {
            get => PimixService.PimixServerApiAddress;
            set => PimixService.PimixServerApiAddress = value;
        }

        public static string PimixServerCredential {
            get => PimixService.PimixServerCredential;
            set => PimixService.PimixServerCredential = value;
        }

        public static bool Patch(Job data, string id = null) => PimixService.Patch(data, id);

        public static bool Post(Job data, string id = null) => PimixService.Post(data, id);

        public static Job Get(string id) => PimixService.Get<Job>(id);

        public static bool Delete(string id) => PimixService.Delete<Job>(id);
    }
}
