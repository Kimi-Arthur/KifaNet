using Pimix.Languages;
using Pimix.Languages.German;
using Xunit;

namespace PimixTest.Languages.German {
    public class DeWiktionaryClientTests {
        [Theory]
        [InlineData("kommen", "komm!", "kommt!", "kommen Sie!")]
        [InlineData("abholen", "hol ab!", "holt ab!", "holen Sie ab!")]
        public void ExtractVerbFormsTest(string id, string imp2s, string imp2p, string impsie) {
            var client = new DeWiktionaryClient();
            var word = client.GetWord(id);
            Assert.Equal(imp2s, word.VerbForms[VerbFormType.Imperative][Person.Du]);
            Assert.Equal(imp2p, word.VerbForms[VerbFormType.Imperative][Person.Ihr]);
            Assert.Equal(impsie, word.VerbForms[VerbFormType.Imperative][Person.Sie]);
        }
    }
}
