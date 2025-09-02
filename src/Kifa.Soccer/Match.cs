using Kifa.Service;

namespace Kifa.Soccer;

public class Match {
    public Competition Competition { get; set; }
    public Season Season { get; set; }
    public Round Round { get; set; }
    public Date Date { get; set; }
    public Team Home { get; set; }
    public Team Away { get; set; }
}
