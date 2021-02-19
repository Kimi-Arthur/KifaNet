using System;
using System.Collections.Generic;
using Kifa.Configs;
using NUnit.Framework;

namespace Kifa.Memrise.Tests {
    public class MemriseClientTests {
        [Test]
        public void AddWordTest() {
            KifaConfigs.LoadFromSystemConfigs();
            using var client =
                new MemriseClient {CourseId = "5942698", CourseName = "test-course", DatabaseId = "6977236"};
            client.AddWord(new MemriseGermanWord {
                Word = "test" + new Random().Next(),
                Meaning = "trash" + new Random().Next(),
                Examples = new List<string> {"abc", "bcd"}
            });
        }

        [Test]
        public void UpdateWordTest() {
            KifaConfigs.LoadFromSystemConfigs();
            using var client =
                new MemriseClient {CourseId = "5942698", CourseName = "test-course", DatabaseId = "6977236"};
            client.AddWord(new MemriseGermanWord {
                Word = "drehen",
                Meaning = "to turn",
                Examples = new List<string> {"abc" + new Random().Next(), "bcd" + new Random().Next()}
            });
        }
    }
}
