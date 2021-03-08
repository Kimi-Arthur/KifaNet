using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Kifa.Languages.German {
    public class DudenClient {
        static Dictionary<string, string> audioLinks;

        static Dictionary<string, string> AudioLinks {
            get {
                if (audioLinks == null) {
                    using var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream($"{typeof(DudenClient).Namespace}.duden_audio.json");
                    if (stream != null) {
                        audioLinks =
                            JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                new StreamReader(stream).ReadToEnd());
                    }
                }

                return audioLinks;
            }
        }

        public Word GetWord(string wordId) =>
            new() {
                Id = wordId,
                PronunciationAudioLinks =
                    new() {{Source.Duden, new List<string> {AudioLinks.GetValueOrDefault(wordId)}}}
            };
    }
}
