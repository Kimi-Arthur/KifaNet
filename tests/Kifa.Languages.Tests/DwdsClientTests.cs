using System.Collections.Generic;
using Kifa.Languages.German;
using Xunit;

namespace Kifa.Languages.Tests {
    public class DwdsClientTests {
        [Theory]
        [InlineData("malen", "https://media.dwds.de/dwds2/audio/019/mahlen.mp3")]
        [InlineData("Ankunft", "https://media.dwds.de/dwds2/audio/004/die_Ankunft.mp3")]
        [InlineData("Laptop", "https://media.dwds.de/dwds2/audio/100/der_Laptop.mp3")]
        [InlineData("ab", null)]
        public void AudioLinkTest(string wordId, string link) {
            var client = new DwdsClient();
            var word = client.GetWord(wordId);
            Assert.Equal(link, word.PronunciationAudioLinks.GetValueOrDefault(Source.Dwds)?[0]);
        }
    }
}
