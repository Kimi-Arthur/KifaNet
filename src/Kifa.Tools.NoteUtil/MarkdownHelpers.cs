using System.Collections.Generic;
using System.Linq;

namespace Kifa.Tools.NoteUtil; 

public class MarkdownHelpers {
    public const string VocabularyTitle = "Vocabulary";
    public const string VerbsTitle = "Verbs";
    public const string NounsTitle = "Nouns";

    public static string[] GetColumnsDefinition(string line) =>
        line.Trim('|').Split("|").Select(x => x.Trim()).ToArray();

    public static string GetWordId(List<string> parts, Dictionary<string, int> columnNames) =>
        parts.Prepend(parts[columnNames["Word"]]).First(x => x != "-").Replace("*", "").Split(" ").Last().Split("(")
            .First();
}