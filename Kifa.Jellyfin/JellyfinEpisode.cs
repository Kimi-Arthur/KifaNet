using System.Xml.Linq;

namespace Kifa.Jellyfin;

// Currently dummy
public class JellyfinEpisode {
    const string SortTitleKey = "sorttitle";
    const string SeasonKey = "season";

    public static bool FixNfo(string path, XDocument document) {
        var root = document.Root.Checked();
        if (root.Name != "episodedetails") {
            return false;
        }

        var modified = false;
        if (root.Element(SortTitleKey)?.Value != path) {
            root.SetElementValue(SortTitleKey, path);
            modified = true;
        }

        if (root.Element(SeasonKey) == null) {
            root.SetElementValue(SeasonKey, 1);
            modified = true;
        }

        return modified;
    }
}
