using System;
using System.Collections.Generic;
using Kifa.Configs;
using Kifa.Languages.German;
using NUnit.Framework;

namespace Kifa.Memrise.Tests {
    public class MemriseClientTests {
        [Test]
        public void AddWordTest() {
            KifaConfigs.LoadFromSystemConfigs();
            using var client =
                new MemriseClient {CourseId = "5942698", CourseName = "test-course", DatabaseId = "6977236"};
            client.AddWord(
                new GoetheGermanWord {
                    Word = "W" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"),
                    Meaning = "M" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"),
                    Examples = new List<string> {"abc", "bcd"}
                },
                new Word {
                    PronunciationAudioLinks = new Dictionary<Source, string> {
                        {Source.Duden, "https://cdn.duden.de/_media_/audio/ID4117087_349083091.mp3"},
                        {Source.Dwds, "https://media.dwds.de/dwds2/audio/004/drehen.mp3"},
                        {Source.Wiktionary, "https://upload.wikimedia.org/wikipedia/commons/a/a8/De-drehen.ogg"},
                        {Source.Pons, "https://sounds.pons.com/audio_tts/de/Tdeen148903"}
                    }
                });
        }

        [Test]
        public void UpdateWordTest() {
            KifaConfigs.LoadFromSystemConfigs();
            using var client =
                new MemriseClient {CourseId = "5942698", CourseName = "test-course", DatabaseId = "6977236"};
            client.AddWord(
                new GoetheGermanWord {
                    Word = "drehen",
                    Meaning = "to turn",
                    Examples = new List<string> {"abc" + new Random().Next(), "bcd" + new Random().Next()}
                }, new Word());
        }
    }
}
