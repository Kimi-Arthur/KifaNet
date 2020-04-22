using System.Linq;
using System.Net.Http;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using WikiClientLibrary.Client;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace Pimix.Languages.German {
    public class EnWiktionaryClient {
        static HttpClient wiktionaryClient = new HttpClient();

        public Word GetWord(string wordId) {
            var client = new WikiClient();
            var site = new WikiSite(client, "https://en.wiktionary.org/w/api.php");
            site.Initialization.Wait();
            var page = new WikiPage(site, "zu");
            page.RefreshAsync(PageQueryOptions.FetchContent).Wait();
            var parser = new WikitextParser();
            var content = parser.Parse(page.Content);
            var inGerman = false;
            foreach (var child in content.Lines) {
                if (child is Heading heading) {
                    if (heading.Level == 2) {
                        if (heading.GetTitle() == "German") {
                            inGerman = true;
                        } else {
                            break;
                        }
                    } else if (heading.Level == 3) {
                        var title = heading.GetTitle();
                        if (title == "Alternative forms" || title == "Etymology" || title == "Pronunciation") {
                            // Do nothing for now.
                        } else {
                            var wordType = ParseWordType(heading.GetTitle());
                            
                        }
                    }
                }
            }

            return new Word();
        }

        static WordType ParseWordType(string id) =>
            id.Split(",").First() switch {
                "Adjective" => WordType.Adjective,
                "Postposition" => WordType.Postposition,
                "Preposition" => WordType.Preposition,
                "Adverb" => WordType.Adverb,
                "Article" => WordType.Article,
                "Conjunction" => WordType.Conjunction,
                "Contraction" => WordType.Contraction,
                "Numeral" => WordType.Numeral,
                "Pronoun" => WordType.Pronoun,
                "Noun" => WordType.Noun,
                "Verb" => WordType.Verb,
                "Proper noun" => WordType.Special,
                _ => WordType.Unknown
            };
    }
}
