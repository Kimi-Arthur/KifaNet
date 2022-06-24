using System.Collections.Generic;
using System.Linq;
using Kifa.Languages.German;
using Xunit;

namespace Kifa.Languages.Tests;

public class DwdsClientTests {
    [Theory]
    [InlineData("malen", "https://media.dwds.de/dwds2/audio/019/mahlen.mp3")]
    [InlineData("Ankunft", "https://media.dwds.de/dwds2/audio/004/die_Ankunft.mp3")]
    [InlineData("Laptop", "https://media.dwds.de/dwds2/audio/100/der_Laptop.mp3")]
    [InlineData("Blei", "https://media.dwds.de/dwds2/audio/007/das_Blei.mp3")]
    [InlineData("ab", "https://media.dwds.de/dwds2/audio/119/ab.mp3")]
    public void AudioLinkTest(string wordId, string link) {
        var client = new DwdsClient();
        var word = client.GetWord(wordId);
        Assert.Equal(link, word.PronunciationAudioLinks.GetValueOrDefault(Source.Dwds)?.First());
    }
}
