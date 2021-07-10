using Kifa.Languages.German;
using Xunit;

namespace Kifa.Languages.Tests {
    public class DeWiktionaryClientTests {
        [Theory]
        [InlineData("kommen", "komm!", "kommt!", "kommen Sie!", "komme", "kommst", "kommt", "kommen", "kommt", "kommen",
            "kam", "kamst", "kam", "kamen", "kamt", "kamen", "bin gekommen", "bist gekommen", "ist gekommen",
            "sind gekommen", "seid gekommen", "sind gekommen", "kommt, kam, ist gekommen")]
        [InlineData("abholen", "hol ab!", "holt ab!", "holen Sie ab!", "hole ab", "holst ab", "holt ab", "holen ab",
            "holt ab", "holen ab", "holte ab", "holtest ab", "holte ab", "holten ab", "holtet ab", "holten ab",
            "habe abgeholt", "hast abgeholt", "hat abgeholt", "haben abgeholt", "habt abgeholt", "haben abgeholt",
            "holt ab, holte ab, hat abgeholt")]
        [InlineData("bedienen", "bedien!", "bedient!", "bedienen Sie!", "bedien", "bedienst", "bedient", "bedienen",
            "bedient", "bedienen", "bediente", "bedientest", "bediente", "bedienten", "bedientet", "bedienten",
            "habe bedient", "hast bedient", "hat bedient", "haben bedient", "habt bedient", "haben bedient",
            "bedient, bediente, hat bedient")]
        [InlineData("unterrichten", "unterrichte!", "unterrichtet!", "unterrichten Sie!", "unterrichte",
            "unterrichtest", "unterrichtet", "unterrichten", "unterrichtet", "unterrichten", "unterrichtete",
            "unterrichtetest", "unterrichtete", "unterrichteten", "unterrichtetet", "unterrichteten",
            "habe unterrichtet", "hast unterrichtet", "hat unterrichtet", "haben unterrichtet", "habt unterrichtet",
            "haben unterrichtet", "unterrichtet, unterrichtete, hat unterrichtet")]
        [InlineData("unternehmen", "unternimm!", "unternehmt!", "unternehmen Sie!", "unternehme", "unternimmst",
            "unternimmt", "unternehmen", "unternehmt", "unternehmen", "unternahm", "unternahmst", "unternahm",
            "unternahmen", "unternahmt", "unternahmen", "habe unternommen", "hast unternommen", "hat unternommen",
            "haben unternommen", "habt unternommen", "haben unternommen", "unternimmt, unternahm, hat unternommen")]
        public void ExtractVerbFormsTest(string id, string imp2s, string imp2p, string impsie, string p1s, string p2s,
            string p3s, string p1p, string p2p, string p3p, string pa1s, string pa2s, string pa3s, string pa1p,
            string pa2p, string pa3p, string pe1s, string pe2s, string pe3s, string pe1p, string pe2p, string pe3p,
            string kf) {
            var client = new DeWiktionaryClient();
            var word = client.GetWord(id);
            word.Meanings.Add(new Meaning() {
                Type = WordType.Verb
            });

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

            Assert.Equal(kf, word.KeyForm);
        }

        [Theory]
        [InlineData("Lernen", "Lernen", null, "Lernens", null, "Lernen", null, "Lernen", null, "(Sg.)")]
        [InlineData("Buch", "Buch", "Bücher", "Buchs", "Bücher", "Buch", "Büchern", "Buch", "Bücher", "¨-er")]
        public void ExtractNounFormsTest(string id, string ns, string np, string gs, string gp, string ds, string dp,
            string @as, string ap, string kf) {
            var client = new DeWiktionaryClient();
            var word = client.GetWord(id);
            word.Meanings.Add(new Meaning() {
                Type = WordType.Noun
            });

            Assert.Equal(ns, word.NounForms[Case.Nominative][Number.Singular]);
            Assert.Equal(kf, word.KeyForm);
        }
    }
}
