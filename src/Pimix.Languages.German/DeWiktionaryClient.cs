using System.Net.Http;
using HtmlAgilityPack;

namespace Pimix.Languages.German {
    public class DeWiktionaryClient {
        static HttpClient wiktionaryClient = new HttpClient();

        public Word GetWord(string wordId) {
            var doc = new HtmlDocument();
            doc.LoadHtml(wiktionaryClient.GetStringAsync($"https://de.wiktionary.org/wiki/{wordId}").Result);
            var pageContentNodes = doc.DocumentNode.SelectSingleNode(".//div[@class='mw-parser-output']").ChildNodes;
            var inDeutsch = false;
            var word = new Word();
            foreach (var node in pageContentNodes) {
                if (inDeutsch) {
                    if (node.Name == "h2") {
                        break;
                    }

                    var ipaNode = node.SelectSingleNode("(.//span[@class='ipa'])[1]");
                    if (ipaNode != null) {
                        word.Pronunciation = ipaNode.InnerText;
                    }

                    var audioNode = node.SelectSingleNode($".//a[@title='De-{wordId}.ogg']");
                    if (audioNode != null) {
                        word.PronunciationAudioLinkWiktionary = audioNode.Attributes["href"].Value;
                    }
                } else if (node.Name == "h2" && node.SelectSingleNode($"./span[@id='{wordId}_(Deutsch)']") != null) {
                    inDeutsch = true;
                }
            }

            return word;
        }
    }
}
