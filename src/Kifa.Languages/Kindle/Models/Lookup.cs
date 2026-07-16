namespace Kifa.Languages.Kindle.Models;

// Represents a lookup (word search) in Kindle's Vocabulary Builder database (`LOOKUPS` table).
//
// Examples of the two types of lookups in the database:
//
// 1. For a Kindle Store Book (Simple Book Key):
//    - Id:        "CR!HNN0JQNBN963QAR3ECETRDDKB9W7:ASsEAACaAAAA:15333:10"
//    - WordKey:   "en:surreal"
//    - BookKey:   "CR!HNN0JQNBN963QAR3ECETRDDKB9W7"
//    - DictKey:   ""
//    - Pos:       "ASsEAACaAAAA:15333"
//    - Usage:     "Already Kelsier could see the mists beginning to form, clouding the air, and giving the moundlike buildings a surreal, intangible look. "
//    - Timestamp: 1682114064613 (DateTimeOffset: 2023-04-21T21:54:24.613Z)
//
// 2. For a Sideloaded Book (Compound Book Key):
//    - Id:        "CR!JZVCM5DBWD59S7MAWG2DGR97B1C1:FB878C50:471571:8"
//    - WordKey:   "en:surreal"
//    - BookKey:   "CR!JZVCM5DBWD59S7MAWG2DGR97B1C1:FB878C50"
//    - DictKey:   ""
//    - Pos:       "471571"
//    - Usage:     "Already Kelsier could see the mists beginning to form, clouding the air, and giving the moundlike buildings a surreal, intangible look. "
//    - Timestamp: 1682114064613
public class Lookup {
    public string Id { get; set; } = "";

    public string? WordKey { get; set; }

    public string? BookKey { get; set; }

    public string? DictKey { get; set; }

    public string? Pos { get; set; }

    public string? Usage { get; set; }

    public long Timestamp { get; set; }
}
