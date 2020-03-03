using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using VerbForms =
    System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType,
        System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;


namespace Pimix.Languages.German {
    public class PonsClient {
        static HttpClient ponsClient = new HttpClient();

        const string SelectField = "data-pons-flection-id";

        static readonly Dictionary<VerbFormType, string> verbFormIds = new Dictionary<VerbFormType, string> {
            [VerbFormType.IndicativePresent] = "INDIKATIV_PRAESENS"
        };

        static readonly Dictionary<Person, string> personIds = new Dictionary<Person, string> {
            [Person.Ich] = "1S",
            [Person.Du] = "2S",
            [Person.Er] = "3S",
            [Person.Wir] = "1P",
            [Person.Ihr] = "2P",
            [Person.Sie] = "3P"
        };

        static readonly Dictionary<string, WordType> wordTypes = new Dictionary<string, WordType> {
            ["verb"] = WordType.Verb,
            ["noun"] = WordType.Noun,
            ["pronoun"] = WordType.Pronoun
        };

        public Word GetWord(string wordId) {
            var doc = new HtmlDocument();
            doc.LoadHtml(ponsClient.GetStringAsync($"https://en.pons.com/translate/german-english/{wordId}").Result);
            var wordNode = doc.DocumentNode.SelectSingleNode("(//div[@class='entry' or @class='entry first'])[1]");
            var type = wordTypes[
                wordNode.SelectSingleNode("//span[@class='wordclass']/acronym[1]").Attributes["title"].Value];

            var word = new Word();
            switch (type) {
                case WordType.Verb:
                    var verb = new Verb();
                    verb.VerbForms = GetVerbForms(wordId);
                    word = verb;
                    break;
            }

            word.Type = type;

            var pronunciationNode = wordNode.SelectSingleNode("(//span[@class='phonetics'])[1]");
            if (pronunciationNode != null) {
                word.Pronunciation = pronunciationNode.InnerText
                    .Split('[', ']', ',')[1];
            }

            var audioLinkNode = wordNode.SelectSingleNode(".//dl[1]");
            if (audioLinkNode != null) {
                word.PronunciationAudioLinkPons = $"https://sounds.pons.com/audio_tts/de/{audioLinkNode.Id}";
            }

            word.Meaning = wordNode.SelectSingleNode("(//div[@class='target'])[1]").InnerText.Trim();
            return word;
        }

        public VerbForms GetVerbForms(string wordId) {
            var forms = new VerbForms();
            var doc = new HtmlDocument();
            doc.LoadHtml(ponsClient.GetStringAsync($"https://en.pons.com/verb-tables/german/{wordId}").Result);
            foreach (VerbFormType form in Enum.GetValues(typeof(VerbFormType))) {
                var x = Enum.GetValues(typeof(Person)).Cast<Person>().ToDictionary(p => p, p => doc
                    .DocumentNode
                    .SelectSingleNode($"(//span[@{SelectField}='{verbFormIds[form]}_{personIds[p]}'])[2]")
                    .InnerText);
                forms[form] = x;
            }

            return forms;
        }
    }
}
