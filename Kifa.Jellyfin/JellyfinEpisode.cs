using System.Xml.Linq;

namespace Kifa.Jellyfin;

// Currently dummy
public class JellyfinEpisode {
    const string SortTitleKey = "sorttitle";
    const string TitleKey = "title";
    const string SeasonKey = "season";

    public static bool FixNfo(string path, XDocument document) {
        var root = document.Root.Checked();
        if (root.Name != "episodedetails") {
            return false;
        }

        var modified = false;
        var title = path.Split("/").Last();
        if (root.Element(SortTitleKey)?.Value != title || root.Element(TitleKey)?.Value != title) {
            root.SetElementValue(SortTitleKey, title);
            root.SetElementValue(TitleKey, title);
            modified = true;
        }

        if (root.Element(SeasonKey) == null) {
            root.SetElementValue(SeasonKey, 1);
            modified = true;
        }

        return modified;
    }
}
