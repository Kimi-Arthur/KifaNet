using System.Net.Http;
using FluentAssertions;

namespace Kifa.ArchiveOrg.Tests;

public class CdxSearchRpcTests {
    static readonly HttpClient Client = new();

    [Fact]
    public void SearchYouTubeArchive() {
        var result = Client.Call(new CdxSearchRpc("https://www.youtube.com/watch?v=0iNrY1ixR8I"));
        result.Should().HaveCountGreaterThan(10);
        result[1][1].Should().Be("20131127172500");
    }
}
