using System.Collections.Generic;
using System.Net.Http;
using HtmlAgilityPack;
using NLog;

namespace Pimix.Languages.German {
    public class DwdsClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static HttpClient dwdsClient = new();

        public Word GetWord(string wordId) {
            var doc = new HtmlDocument();
            using var response = dwdsClient.GetAsync($"https://www.dwds.de/wb/{wordId}").Result;
            doc.LoadHtml(response.GetString());
            var audioNodes = doc.DocumentNode.SelectNodes("//audio/source");

            var word = new Word {PronunciationAudioLinks = new Dictionary<Source, string>()};

            if (audioNodes?.Count > 0) {
                word.PronunciationAudioLinks[Source.Dwds] = $"https:{audioNodes[0].Attributes["src"].Value}";
            }

            return word;
        }
    }
}
