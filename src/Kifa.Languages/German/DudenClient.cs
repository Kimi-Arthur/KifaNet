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
            new Word {
                Id = wordId,
                PronunciationAudioLinks =
                    new Dictionary<Source, string> {{Source.Duden, AudioLinks.GetValueOrDefault(wordId)}}
            };
    }
}
