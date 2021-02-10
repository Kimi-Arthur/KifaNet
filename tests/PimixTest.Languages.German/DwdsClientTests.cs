using Pimix.Languages.German;
using Xunit;

namespace PimixTest.Languages.German {
    public class DwdsClientTests {
        [Theory]
        [InlineData("malen", "https://media.dwds.de/dwds2/audio/019/mahlen.mp3")]
        [InlineData("Ankunft", "https://media.dwds.de/dwds2/audio/004/die_Ankunft.mp3")]
        public void AudioLinkTest(string wordId, string link) {
            var client = new DwdsClient();
            var word = client.GetWord(wordId);
            Assert.Equal(link, word.PronunciationAudioLinks[Source.Dwds]);
        }
    }
}
