using System.Collections.Generic;

namespace Kifa.Infos;

public interface ItemProvider {
    IEnumerable<ItemInfo>? GetItems(List<string> spec);
}
