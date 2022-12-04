using System.Net.Http;
using FluentAssertions;
using Kifa.Configs;
using Kifa.Languages.Moji.Rpcs;
using Xunit;

namespace Kifa.Languages.Tests;

public class MojiTests {
    static HttpClient HttpClient = new HttpClient();

    public MojiTests() {
        KifaConfigs.Init();
    }

    [Theory]
    [InlineData("鉛筆", 5, "鉛筆 | えんぴつ ◎", "198934326")]
    [InlineData("车", 5, "車 | くるま ◎", "198962739")]
    public void MojiSearchRpcTests(string word, int count, string firstTitle, string firstTarId) {
        var results = HttpClient.Call(new MojiSearchRpc(word))!.Result.SearchResults;
        results.Should().HaveCount(count);
        Assert.Equal(firstTitle, results[0].Title);
        Assert.Equal(firstTarId, results[0].TarId);
    }
}
