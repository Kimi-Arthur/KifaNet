using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    partial class Job {
        public static bool Patch(Job data, string id = null) => PimixService.Patch(data, id);

        public static bool Post(Job data, string id = null) => PimixService.Post(data, id);

        public static Job Get(string id) => PimixService.Get<Job>(id);

        public static bool Delete(string id) => PimixService.Delete<Job>(id);
    }
}
