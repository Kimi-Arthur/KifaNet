using Pimix.Languages.German;
using Xunit;

namespace PimixTest.Languages.German {
    public class PonsClientTests {
        [Fact]
        public void VerbFormsTest() {
            var client = new PonsClient();
            var forms = client.GetVerbForms("malen");
            Assert.Equal("male", forms[VerbFormType.PresentIndicative][Person.Ich]);
        }

        [Fact]
        public void VerbTest() {
            var client = new PonsClient();
            var verb = client.GetWord("malen");
            Assert.IsType<Verb>(verb);
            Assert.Equal("to paint", verb.Translation);
            Assert.Equal("[ˈma:lən]", verb.PronunciationIpa);
        }
    }
}
