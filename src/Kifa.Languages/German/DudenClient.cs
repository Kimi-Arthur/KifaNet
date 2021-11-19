using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Kifa.Languages.German {
    public class DudenClient {
        static Dictionary<string, string>? audioLinks;

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

                return audioLinks!;
            }
        }

        public GermanWord GetWord(string wordId) =>
            new() {
                Id = wordId,
                PronunciationAudioLinks = new Dictionary<Source, HashSet<string>> {
                    {
                        Source.Duden, AudioLinks.ContainsKey(wordId)
                            ? new HashSet<string> {
                                AudioLinks[wordId]
                            }
                            : new HashSet<string>()
                    }
                }
            };
    }
}
