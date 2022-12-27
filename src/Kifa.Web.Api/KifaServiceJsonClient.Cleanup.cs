using System.Collections.Generic;

namespace Kifa.Web.Api;

public partial class KifaServiceJsonClient<TDataModel> {
    public void FixVirtualLinks() {
        foreach (var item in List().Values) {
            WriteVirtualItems(item, new SortedSet<string>());
        }
    }
}
