using Pimix.Languages.German;
using Xunit;

namespace PimixTest.Languages.German {
    public class DeWiktionaryClientTests {
        [Theory]
        [InlineData("kommen", "komm!", "kommt!", "kommen Sie!", "komme", "kommst", "kommt", "kommen", "kommt", "kommen",
            "kam", "kamst", "kam", "kamen", "kamt", "kamen", "bin gekommen", "bist gekommen", "ist gekommen",
            "sind gekommen", "seid gekommen", "sind gekommen", new[] {"kommt", "kam", "ist gekommen"})]
        [InlineData("abholen", "hol ab!", "holt ab!", "holen Sie ab!", "hole ab", "holst ab", "holt ab", "holen ab",
            "holt ab", "holen ab", "holte ab", "holtest ab", "holte ab", "holten ab", "holtet ab", "holten ab",
            "habe abgeholt", "hast abgeholt", "hat abgeholt", "haben abgeholt", "habt abgeholt", "haben abgeholt",
            new[] {"holt ab", "holte ab", "hat abgeholt"})]
        public void ExtractVerbFormsTest(string id, string imp2s, string imp2p, string impsie, string p1s, string p2s,
            string p3s, string p1p, string p2p, string p3p, string pa1s, string pa2s, string pa3s, string pa1p,
            string pa2p, string pa3p, string pe1s, string pe2s, string pe3s, string pe1p, string pe2p, string pe3p,
            string[] kf) {
            var client = new DeWiktionaryClient();
            var word = client.GetWord(id);

            Assert.Equal(imp2s, word.VerbForms[VerbFormType.Imperative][Person.Du]);
            Assert.Equal(imp2p, word.VerbForms[VerbFormType.Imperative][Person.Ihr]);
            Assert.Equal(impsie, word.VerbForms[VerbFormType.Imperative][Person.Sie]);

            Assert.Equal(p1s, word.VerbForms[VerbFormType.IndicativePresent][Person.Ich]);
            Assert.Equal(p2s, word.VerbForms[VerbFormType.IndicativePresent][Person.Du]);
            Assert.Equal(p3s, word.VerbForms[VerbFormType.IndicativePresent][Person.Er]);
            Assert.Equal(p1p, word.VerbForms[VerbFormType.IndicativePresent][Person.Wir]);
            Assert.Equal(p2p, word.VerbForms[VerbFormType.IndicativePresent][Person.Ihr]);
            Assert.Equal(p3p, word.VerbForms[VerbFormType.IndicativePresent][Person.Sie]);

            Assert.Equal(pa1s, word.VerbForms[VerbFormType.IndicativePreterite][Person.Ich]);
            Assert.Equal(pa2s, word.VerbForms[VerbFormType.IndicativePreterite][Person.Du]);
            Assert.Equal(pa3s, word.VerbForms[VerbFormType.IndicativePreterite][Person.Er]);
            Assert.Equal(pa1p, word.VerbForms[VerbFormType.IndicativePreterite][Person.Wir]);
            Assert.Equal(pa2p, word.VerbForms[VerbFormType.IndicativePreterite][Person.Ihr]);
            Assert.Equal(pa3p, word.VerbForms[VerbFormType.IndicativePreterite][Person.Sie]);

            Assert.Equal(pe1s, word.VerbForms[VerbFormType.IndicativePerfect][Person.Ich]);
            Assert.Equal(pe2s, word.VerbForms[VerbFormType.IndicativePerfect][Person.Du]);
            Assert.Equal(pe3s, word.VerbForms[VerbFormType.IndicativePerfect][Person.Er]);
            Assert.Equal(pe1p, word.VerbForms[VerbFormType.IndicativePerfect][Person.Wir]);
            Assert.Equal(pe2p, word.VerbForms[VerbFormType.IndicativePerfect][Person.Ihr]);
            Assert.Equal(pe3p, word.VerbForms[VerbFormType.IndicativePerfect][Person.Sie]);

            Assert.Equal(kf, word.KeyVerbForms);
        }
    }
}
