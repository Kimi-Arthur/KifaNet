using Pimix.Service;

namespace Pimix.Cloud.BaiduCloud {
    public partial class BaiduCloudConfig {
        public static string PimixServerApiAddress {
            get => PimixService.PimixServerApiAddress;
            set => PimixService.PimixServerApiAddress = value;
        }

        public static string PimixServerCredential {
            get => PimixService.PimixServerCredential;
            set => PimixService.PimixServerCredential = value;
        }

        public static bool Patch(BaiduCloudConfig data, string id = null)
            => PimixService.Patch(data, id);

        public static bool Post(BaiduCloudConfig data, string id = null)
            => PimixService.Post(data, id);

        public static BaiduCloudConfig Get(string id) => PimixService.Get<BaiduCloudConfig>(id);

        public static bool Delete(string id) => PimixService.Delete<BaiduCloudConfig>(id);
    }
}
