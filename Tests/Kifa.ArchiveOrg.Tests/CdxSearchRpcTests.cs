using System.Net.Http;
using FluentAssertions;

namespace Kifa.ArchiveOrg.Tests;

public class CdxSearchRpcTests {
    static readonly HttpClient Client = new();

    [Fact]
    public void SearchYouTubeArchive() {
        var result = Client.Call(new CdxSearchRpc("https://www.youtube.com/watch?v=0iNrY1ixR8I"));
        result.Should().HaveCountGreaterThan(10);
        result[0].ToJson().Should().Be(new CdxSearchRpc.ArchiveEntry {
            UrlKey = "com,youtube)/watch?v=0inry1ixr8i",
            Timestamp = "20131127172500",
            Original = "http://www.youtube.com/watch?v=0iNrY1ixR8I&gl=US&hl=en",
            MimeType = "text/html",
            StatusCode = 200,
            Digest = "5P3KHGI3SVWPJKIA7T4TGL3PQAVUWKVQ",
            Length = 24444
        }.ToJson());
    }
}
