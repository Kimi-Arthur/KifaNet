using System.Collections.Generic;
using System.Linq;
using Kifa.Languages.German;
using Xunit;

namespace Kifa.Languages.Tests;

public class DwdsClientTests {
    [Theory]
    [InlineData("malen", "https://www.dwds.de/audio/019/mahlen.mp3")]
    [InlineData("Ankunft", "https://www.dwds.de/audio/004/die_Ankunft.mp3")]
    [InlineData("Laptop", "https://www.dwds.de/audio/100/der_Laptop.mp3")]
    [InlineData("Blei", "https://www.dwds.de/audio/007/das_Blei.mp3")]
    [InlineData("ab", "https://www.dwds.de/audio/119/ab.mp3")]
    public void AudioLinkTest(string wordId, string link) {
        var client = new DwdsClient();
        var word = client.GetWord(wordId);
        Assert.Equal(link, word.PronunciationAudioLinks.GetValueOrDefault(Source.Dwds)?.First());
    }
}
