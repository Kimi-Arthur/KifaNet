using System.Net.Http;
using FluentAssertions;

namespace Kifa.ArchiveOrg.Tests;

public class ArchiveContentRpcTests {
    static readonly HttpClient Client = new();

    [Fact]
    public void ContentRpcTest() {
        var result =
            Client.Call(new ArchiveContentRpc(
                "http://www.youtube.com/watch?v=0iNrY1ixR8I&gl=US&hl=en", "20131127172500"));
        result.Should().Contain("Red Carpet Interview");
    }
}
