using System;
using System.Collections.Generic;
using Kifa.Configs;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Service;
using NUnit.Framework;

namespace Kifa.Memrise.Tests;

public class MemriseClientTests {
    static readonly MemriseCourse TestCourse = new() {
        CourseId = "5946002",
        CourseName = "goethe-zertifikat-wortliste",
        DatabaseId = "6985350",
        Levels = new Dictionary<string, string> {
            { "test", "13341759" },
            { "A2", "13341763" },
            { "B1", "13341765" }
        },
        Columns = new Dictionary<string, string> {
            { "German", "1" },
            { "English", "2" },
            { "Audios", "7" },
            { "Etymology", "9" },
            { "Form", "10" },
            { "Pronunciation", "11" },
            { "Cambridge", "12" },
            { "Examples", "8" },
            { "Wiki", "13" }
        }
    };

    [Test]
    public void AddWordTest() {
        KifaConfigs.LoadFromSystemConfigs();
        TestCourse.Fill();
        using var client = new MemriseClient {
            Course = TestCourse
        };

        var id = "W" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var goetheGermanWord = new GoetheGermanWord {
            Id = id,
            Meaning = "M" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"),
            Examples = new List<string> {
                "abc",
                "bcd"
            }
        };

        var germanWord = new GermanWord {
            Id = id,
            PronunciationAudioLinks = new Dictionary<Source, HashSet<string>> {
                {
                    Source.Duden, new HashSet<string> {
                        "https://cdn.duden.de/_media_/audio/ID4117087_349083091.mp3"
                    }
                }, {
                    Source.Dwds, new HashSet<string> {
                        "https://media.dwds.de/dwds2/audio/004/drehen.mp3"
                    }
                }, {
                    Source.Wiktionary, new HashSet<string> {
                        "https://upload.wikimedia.org/wikipedia/commons/a/a8/De-drehen.ogg"
                    }
                }, {
                    Source.Pons, new HashSet<string> {
                        "https://sounds.pons.com/audio_tts/de/Tdeen148903"
                    }
                }
            }
        };

        var result = client.AddWord(goetheGermanWord, germanWord);

        Assert.AreEqual(KifaActionStatus.OK, result.Status, result.Message);

        client.AddWord(goetheGermanWord, germanWord);
    }

    [Test]
    public void UpdateWordTest() {
        KifaConfigs.LoadFromSystemConfigs();

        TestCourse.Fill();
        using var client = new MemriseClient {
            Course = TestCourse
        };
        var result = client.AddWord(new GoetheGermanWord {
            Id = "drehen",
            Meaning = "to turn",
            Examples = new List<string> {
                "abc" + new Random().Next(),
                "bcd" + new Random().Next()
            }
        }, new GermanWord {
            Id = "drehen"
        });

        // TODO: Check updated word.

        Assert.AreEqual(KifaActionStatus.OK, result.Status, result.Message);
    }

    [Test]
    public void AddWordListTest() {
        KifaConfigs.LoadFromSystemConfigs();
        using var client = new MemriseClient {
            Course = TestCourse
        };
        var result = client.AddWordList(new GoetheWordList {
            Id = "test",
            Words = new List<string> {
                "drehen",
                "abbiegen",
                "außen"
            }
        });
    }

    [Test]
    public void GetAllWordsTest() {
        KifaConfigs.LoadFromSystemConfigs();
        TestCourse.Fill();
        Assert.NotZero(TestCourse.Words.Count);
    }
}
