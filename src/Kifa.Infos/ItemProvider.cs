using System.Collections.Generic;

namespace Kifa.Infos;

public interface ItemProvider {
    static abstract ItemInfoList? GetItems(string[] spec, string? version = null);
}
