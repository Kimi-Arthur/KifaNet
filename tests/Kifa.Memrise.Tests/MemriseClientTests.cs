using System;
using Kifa.Configs;
using NUnit.Framework;

namespace Kifa.Memrise.Tests {
    public class MemriseClientTests {
        [Test]
        public void AddWordTest() {
            KifaConfigs.LoadFromSystemConfigs();
            var client = new MemriseClient {CourseId = "5942698", CourseName = "test-course", DatabaseId = "6977236"};
            client.AddWord(new MemriseGermanWord {
                Word = "test" + new Random().Next(), Meaning = "trash" + new Random().Next()
            });
        }
    }
}
