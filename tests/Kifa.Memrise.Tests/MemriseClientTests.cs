using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Configs;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Service;
using NUnit.Framework;

namespace Kifa.Memrise.Tests;

public class MemriseClientTests {
    public MemriseCourse TestCourse { get; set; }

    public MemriseClientTests() {
        KifaConfigs.LoadFromSystemConfigs();
        TestCourse = MemriseCourse.Client.Get("test-course");
    }

    [Test]
    public void AddWordTest() {
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
        var newExample1 = "abc" + new Random().Next();
        var newExample2 = "bcd" + new Random().Next();
        using var client = new MemriseClient {
            Course = TestCourse
        };
        var result = client.AddWord(new GoetheGermanWord {
            Id = "drehen",
            Meaning = "to turn",
            Examples = new List<string> {
                newExample1,
                newExample2
            }
        }, new GermanWord {
            Id = "drehen"
        });
        Assert.AreEqual(KifaActionStatus.OK, result.Status, result.Message);

        TestCourse.Fill();
        var word = TestCourse.Words.First(w => w.Key == "drehen").Value;
        Assert.AreEqual($"1. {newExample1} 2. {newExample2}",
            word.Data.Data[TestCourse.Columns["Examples"]]);
    }

    [Test]
    public void AddWordListTest() {
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
        Assert.AreEqual(KifaActionStatus.OK, result.Status, result.Message);
    }

    [Test]
    public void GetAllWordsTest() {
        Assert.NotZero(TestCourse.Words.Count);
    }

    [Test]
    public void GetLevelTest() {
        var level = new MemriseLevel {
            Id = $"{TestCourse.Id}/{TestCourse.Levels["test"]}"
        };
        level.Fill();
        Assert.NotZero(level.Words.Count);
    }
}
