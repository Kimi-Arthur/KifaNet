using Pimix.Service;

namespace Pimix.IO {
    public partial class FileInformation {
        public static string PimixServerApiAddress {
            get => PimixService.PimixServerApiAddress;
            set => PimixService.PimixServerApiAddress = value;
        }

        public static string PimixServerCredential {
            get => PimixService.PimixServerCredential;
            set => PimixService.PimixServerCredential = value;
        }

        public static bool Patch(FileInformation data, string id = null)
            => PimixService.Patch(data, id);

        public static bool Post(FileInformation data, string id = null)
            => PimixService.Post(data, id);

        public static FileInformation Get(string id) => PimixService.Get<FileInformation>(id);

        public static bool Link(string targetId, string linkId)
            => PimixService.Link<FileInformation>(targetId, linkId);

        public static bool Delete(string id) => PimixService.Delete<FileInformation>(id);
    }
}
