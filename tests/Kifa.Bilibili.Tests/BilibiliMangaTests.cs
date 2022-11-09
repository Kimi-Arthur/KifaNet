using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliMangaTests {
    static readonly HttpClient HttpClient = new();

    [Theory]
    [InlineData("25900", "总之就是非常可爱", "畑健二郎",
        "擅长学习但是有点脱线的主人公由崎星空在某一天对神秘的美少女司一见钟情。\n面对星空豁出去的告白，她的回答是——\n“如果你愿意和我结婚，那我就跟你交往”?!\n与充满谜团但总之就是非常可爱的妻子的新婚生活开始了!!",
        "月光传递着爱的信息", 214)]
    public void MediaRpcTest(string mangaId, string title, string author, string description,
        string firstEpisodeTitle, int minEpisodesCount) {
        var result =
            HttpClient.SendWithRetry<BilibiliMangaResponse>(new BilibiliMangaRequest(mangaId));
        var data = result.Data;
        Assert.Equal(title, data.Title);
        Assert.Equal(author, data.AuthorName[0]);
        Assert.Equal(description, data.Evaluate);
        Assert.Equal(firstEpisodeTitle, data.EpList[^1].Title);
        Assert.True(data.EpList.Length >= minEpisodesCount);
    }
}
