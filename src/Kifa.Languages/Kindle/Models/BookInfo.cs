namespace Kifa.Languages.Kindle.Models;

// Represents a book in Kindle's Vocabulary Builder database (`BOOK_INFO` table).
//
// Examples of the two types of books in the database:
//
// 1. Kindle Store Book:
//    - Id:      "CR!3158VZVEGD6BZDPFY7BAYXJXCXTS"
//    - Asin:    "B000FC1PWA"
//    - Guid:    "CR!3158VZVEGD6BZDPFY7BAYXJXCXTS"
//    - Lang:    "en"
//    - Title:   "Foundation"
//    - Authors: "Asimov, Isaac"
//
// 2. Personal Document / Sideloaded Book:
//    - Id:      "CR!JZVCM5DBWD59S7MAWG2DGR97B1C1:FB878C50"
//    - Asin:    "HBMRAFEWTSY2V25BAWVCKKLTCFUEMJUL"
//    - Guid:    "CR!JZVCM5DBWD59S7MAWG2DGR97B1C1:FB878C50"
//    - Lang:    "en"
//    - Title:   "A Clash of Kings"
//    - Authors: "George R. R. Martin"
public class BookInfo {
    public string Id { get; set; } = "";

    public string? Asin { get; set; }

    public string? Guid { get; set; }

    public string? Lang { get; set; }

    public string? Title { get; set; }

    public string? Authors { get; set; }
}
