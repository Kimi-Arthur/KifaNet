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

    [Theory]
    [InlineData("198934326", "鉛筆", "名詞", "えんぴつ", "◎",
        "铅笔。（筆記具の一種。黒鉛と粘土との粉末の混合物を高熱で焼いて芯を造り、木の軸にはめて造る。1565年、イギリスで考案。江戸初期にオランダ人から輸入。）", 8)]
    [InlineData("198962739", "車", "名", "くるま", "◎", "车，小汽车。（自動車・人力車。）", 8)]
    public void MojiGetWordRpcTests(string wordId, string word, string type, string pronunciation,
        string accent, string meaning, int exampleCount) {
        var result = HttpClient.Call(new MojiGetWordRpc(wordId))!.Result.Result[0];
        Assert.Equal(wordId, result.Word.ObjectId);
        Assert.Equal(word, result.Word.Spell);
        Assert.Equal(type, result.Details[0].Title);
        Assert.Equal(pronunciation, result.Word.Pron);
        Assert.Equal(accent, result.Word.Accent);
        result.Examples.Should().HaveCount(exampleCount);
    }
}
