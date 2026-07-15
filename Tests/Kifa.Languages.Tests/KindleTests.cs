using System.Linq;
using FluentAssertions;
using Kifa.Languages.Kindle;
using Xunit;

namespace Kifa.Languages.Tests;

public class KindleTests {
    public string VocabFile { get; set; } = "vocab.db";

    [Fact]
    public void LoadBookTest() {
        var books = KindleVocabReader.GetBooks(VocabFile);
        books.Should().HaveCount(35);

        var foundationBook = books.Single(b => b.Id == "CR!3158VZVEGD6BZDPFY7BAYXJXCXTS");
        foundationBook.Asin.Should().Be("B000FC1PWA");
        foundationBook.Guid.Should().Be("CR!3158VZVEGD6BZDPFY7BAYXJXCXTS");
        foundationBook.Lang.Should().Be("en");
        foundationBook.Title.Should().Be("Foundation");
        foundationBook.Authors.Should().Be("Asimov, Isaac");
    }

    [Fact]
    public void LoadLookupTest() {
        var lookups = KindleVocabReader.GetLookups(VocabFile);
        lookups.Should().HaveCount(3608);

        var surrealLookup = lookups.Single(l => l.Id == "CR!HNN0JQNBN963QAR3ECETRDDKB9W7:ASsEAACaAAAA:15333:10");
        surrealLookup.WordKey.Should().Be("en:surreal");
        surrealLookup.BookKey.Should().Be("CR!HNN0JQNBN963QAR3ECETRDDKB9W7");
        surrealLookup.DictKey.Should().BeNullOrEmpty();
        surrealLookup.Pos.Should().Be("ASsEAACaAAAA:15333");
        surrealLookup.Usage.Should().Contain("Already Kelsier could see the mists beginning to form");
        surrealLookup.Timestamp.Should().Be(1682114064613);
    }
}
