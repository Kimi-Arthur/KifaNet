using System.Xml.Linq;

namespace Kifa.Jellyfin;

// Currently dummy
public class JellyfinMovie {
    public string Title { get; set; }
    public string Plot { get; set; }
    public string Studio { get; set; }
    public string Director { get; set; }
    public List<string> Actor { get; set; }
    public Date Year { get; set; }
    public List<string> Genre { get; set; }
    public List<string> Tag { get; set; }
    public string Thumb { get; set; }
    public string Fanart { get; set; }
}
