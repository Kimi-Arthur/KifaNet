using System.Collections.Generic;

namespace Kifa.Infos;

public interface ItemProvider {
    static abstract IEnumerable<ItemInfo>? GetItems(string[] spec);
}
