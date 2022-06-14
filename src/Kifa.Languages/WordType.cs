namespace Kifa.Languages;

public enum WordType {
    Unknown,
    Adjective,
    Adverb,
    Article,
    Conjunction,
    Contraction,
    Noun,
    Numeral,
    Postposition,
    Preposition,
    Pronoun,
    Verb,
    Particle,
    Interjection,
    ProperNoun
}

public static class WordTypeExtensions {
    public static string GetShort(this WordType type)
        => type switch {
            WordType.Adjective => "adj.",
            WordType.Adverb => "adv.",
            WordType.Article => "art.",
            WordType.Conjunction => "conj.",
            WordType.Noun => "n.",
            WordType.Preposition => "prep.",
            WordType.Pronoun => "pron.",
            WordType.Verb => "v.",
            WordType.Interjection => "int.",
            _ => type.ToString().ToLowerInvariant()
        };
}
