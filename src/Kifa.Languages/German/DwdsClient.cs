using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using NLog;

namespace Kifa.Languages.German {
    public class DwdsClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static HttpClient dwdsClient = new();

        public GermanWord GetWord(string wordId) {
            var doc = new HtmlDocument();
            using var response = dwdsClient.GetAsync($"https://www.dwds.de/wb/{wordId}").Result;
            doc.LoadHtml(response.GetString());
            var audioNodes = doc.DocumentNode.SelectNodes("//audio/source");

            var word = new GermanWord {PronunciationAudioLinks = new Dictionary<Source, List<string>>()};

            if (audioNodes?.Count > 0) {
                word.PronunciationAudioLinks[Source.Dwds] =
                    audioNodes.Select(audioNode => $"https:{audioNode.Attributes["src"].Value}").ToList();
            }

            return word;
        }
    }
}
