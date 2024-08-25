using System.Collections.Generic;

namespace Kifa.Infos;

public interface FolderLinkable {
    public IEnumerable<string> FolderLinks { get; }
}
