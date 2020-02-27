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

        const string selectField = "data-pons-flection-id";

        static readonly Dictionary<VerbFormType, string> verbFormIds = new Dictionary<VerbFormType, string> {
            [VerbFormType.PresentIndicative] = "INDIKATIV_PRAESENS"
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

        public Word GetWord(string word) {
            var doc = new HtmlDocument();
            doc.LoadHtml(ponsClient.GetStringAsync($"https://en.pons.com/translate/german-english/{word}").Result);
            var wordNode = doc.DocumentNode.SelectSingleNode("//div[@class='entry first']");
            var type = wordTypes[wordNode.SelectSingleNode("//acronym[1]").Attributes["title"].Value];

            Word myWord = new Word();
            switch (type) {
                case WordType.Verb:
                    var verb = new Verb();
                    verb.VerbForms = GetVerbForms(word);
                    myWord = verb;
                    break;
            }

            myWord.Type = type;
            myWord.PronunciationIpa = wordNode.SelectSingleNode("(//span[@class='phonetics'])[1]").InnerText;
            myWord.Translation = wordNode.SelectSingleNode("(//div[@class='target'])[1]").InnerText.Trim();
            return myWord;
        }

        public VerbForms GetVerbForms(string word) {
            var forms = new VerbForms();
            var doc = new HtmlDocument();
            doc.LoadHtml(ponsClient.GetStringAsync($"https://en.pons.com/verb-tables/german/{word}").Result);
            foreach (VerbFormType form in Enum.GetValues(typeof(VerbFormType))) {
                var x = Enum.GetValues(typeof(Person)).Cast<Person>().ToDictionary(p => p, p => doc
                    .DocumentNode
                    .SelectSingleNode($"(//span[@{selectField}='{verbFormIds[form]}_{personIds[p]}'])[2]")
                    .InnerText);
                forms[form] = x;
            }

            return forms;
        }
    }
}
