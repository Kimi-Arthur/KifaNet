using Pimix.Languages;
using Pimix.Languages.German;
using Xunit;

namespace PimixTest.Languages.German {
    public class EnWiktionaryClientTests {
        [Fact]
        public void ExtractMeaningTest() {
            var client = new EnWiktionaryClient();
            var word = client.GetWord("Saft");
            Assert.Equal(6, word.Meanings.Count);
            Assert.Equal("juice", word.Meaning);
            var meaning = word.Meanings[0];
            Assert.Equal(WordType.Noun, meaning.Type);
            Assert.Equal("juice", meaning.Translation);
            Assert.Equal("(beverage) juice", meaning.TranslationWithNotes);
        }
    }
}
