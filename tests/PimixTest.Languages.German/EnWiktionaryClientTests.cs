using Pimix.Languages.German;
using Xunit;

namespace PimixTest.Languages.German {
    public class EnWiktionaryClientTests {
        [Fact]
        public void VerbFormsTest() {
            var client = new EnWiktionaryClient();
            var word = client.GetWord("malen");
            Assert.Equal("male", word.Meaning);
        }
    }
}
