using Kifa.Languages.German;
using Xunit;

namespace Kifa.Languages.Tests;

public class EnWiktionaryClientTests {
    [Fact]
    public void ExtractMeaningTest() {
        var client = new EnWiktionaryClient();
        var word = client.GetWord("Saft");
        Assert.Equal(7, word.Meanings.Count);
        var meaning = word.Meanings[0] as WikiMeaning;
        Assert.Equal(WordType.Noun, meaning.Type);
        Assert.Equal("juice", meaning.Translation);
        Assert.Equal("juice", meaning.RawTranslation);
        Assert.Equal("(beverage) juice", meaning.TranslationWithNotes);
    }

    [Fact]
    public void ExtractMeaningWithExamplesTest() {
        var client = new EnWiktionaryClient();
        var word = client.GetWord("zu");
        Assert.Equal(22, word.Meanings.Count);
        Assert.Equal("for; as, by way of", word.Meanings[6].Translation);
        var meaning = word.Meanings[0] as WikiMeaning;
        Assert.Equal(WordType.Preposition, meaning.Type);
        Assert.Equal("to, towards", meaning.Translation);
        Assert.Equal("to, towards", meaning.RawTranslation);
        Assert.Equal("to, towards (indicates directionality)", meaning.TranslationWithNotes);
        var example = meaning.Examples[0];
        Assert.Equal("zum Bahnhof", example.Text);
        Assert.Equal("to the train station", example.Translation);
    }

    [Fact]
    public void ExtractSuffixTest() {
        var client = new EnWiktionaryClient();
        var word = client.GetWord("-er");
        Assert.Equal(6, word.Meanings.Count);
        Assert.Equal("Forms agent nouns etc. from verbs, suffixed to the verb stem.",
            (word.Meanings[0] as WikiMeaning).TranslationWithNotes);
    }
}
