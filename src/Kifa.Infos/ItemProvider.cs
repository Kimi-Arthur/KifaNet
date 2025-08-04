using System.Collections.Generic;

namespace Kifa.Infos;

public interface ItemProvider {
    static abstract ItemsInfo? GetItems(string[] spec);
}
