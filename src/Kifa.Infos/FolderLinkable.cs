using System.Collections.Generic;

namespace Kifa.Infos;

public interface FolderLinkable {
    IEnumerable<string> FolderLinks { get; }
}
