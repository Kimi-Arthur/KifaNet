using Pimix.Service;

namespace Pimix.Cloud.MegaNz {
    public partial class MegaNzConfig {
        public static bool Patch(MegaNzConfig data, string id = null)
            => PimixService.Patch(data, id);

        public static bool Post(MegaNzConfig data, string id = null) => PimixService.Post(data, id);

        public static MegaNzConfig Get(string id) => PimixService.Get<MegaNzConfig>(id);

        public static bool Delete(string id) => PimixService.Delete<MegaNzConfig>(id);
    }
}
