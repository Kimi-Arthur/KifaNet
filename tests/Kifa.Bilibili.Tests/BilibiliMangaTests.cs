using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliMangaTests {
    static readonly HttpClient HttpClient = new();

    public BilibiliMangaTests() {
        KifaConfigs.Init();
    }

    [Theory]
    [InlineData("25900", "总之就是非常可爱", "畑健二郎",
        "擅长学习但是有点脱线的主人公由崎星空在某一天对神秘的美少女司一见钟情。\n面对星空豁出去的告白，她的回答是——\n“如果你愿意和我结婚，那我就跟你交往”?!\n与充满谜团但总之就是非常可爱的妻子的新婚生活开始了!!",
        "月光传递着爱的信息", 214)]
    [InlineData("28284", "迷宫饭", "九井谅子",
        "【此漫画的翻译由版权方提供】探险者莱欧斯一行人在地下迷宫深层遭遇了强大的红龙，因为红龙的袭击，他们失去了金钱和食物。尽管他们想再次挑战探险迷宫，但就这样贸然继续前进，很可能中途就会饿死……这时莱欧斯决定：“我们干脆吃魔物吧！”于是，不论攻打过来的魔物是史莱姆、翼蜥还是恶龙，一行人边把它们做成“魔物餐”，边踏上攻略迷宫的旅程。\n",
        "汤锅", 76)]
    public void MediaRpcTest(string mangaId, string title, string author, string description,
        string firstEpisodeTitle, int minEpisodesCount) {
        var result = HttpClient.Call(new BilibiliMangaRpc(mangaId));
        var data = result.Data;
        Assert.Equal(title, data.Title);
        Assert.Equal(author, data.AuthorName[0]);
        Assert.Equal(description, data.Evaluate);
        Assert.Equal(firstEpisodeTitle, data.EpList[^1].Title);
        Assert.True(data.EpList.Length >= minEpisodesCount);
    }

    [Theory]
    [InlineData("mc25900", "总之就是非常可爱", "畑健二郎",
        "擅长学习但是有点脱线的主人公由崎星空在某一天对神秘的美少女司一见钟情。\n面对星空豁出去的告白，她的回答是——\n“如果你愿意和我结婚，那我就跟你交往”?!\n与充满谜团但总之就是非常可爱的妻子的新婚生活开始了!!",
        "月光传递着爱的信息", 214)]
    [InlineData("mc28284", "迷宫饭", "九井谅子",
        "【此漫画的翻译由版权方提供】探险者莱欧斯一行人在地下迷宫深层遭遇了强大的红龙，因为红龙的袭击，他们失去了金钱和食物。尽管他们想再次挑战探险迷宫，但就这样贸然继续前进，很可能中途就会饿死……这时莱欧斯决定：“我们干脆吃魔物吧！”于是，不论攻打过来的魔物是史莱姆、翼蜥还是恶龙，一行人边把它们做成“魔物餐”，边踏上攻略迷宫的旅程。\n",
        "汤锅", 76)]
    public void MediaFillTest(string mangaId, string title, string author, string description,
        string firstEpisodeTitle, int minEpisodesCount) {
        var manga = new BilibiliManga {
            Id = mangaId
        };

        manga.Fill();
        Assert.Equal(title, manga.Title);
        Assert.Equal(author, manga.Authors[0]);
        Assert.Equal(description, manga.Description);
        Assert.Equal(firstEpisodeTitle, manga.Episodes[0].Title);
        Assert.True(manga.Episodes.Count >= minEpisodesCount);
    }

    public static IEnumerable<object[]> GetImageDownloadTestData
        => new List<object[]> {
            new object[] {
                new List<string> {
                    "83a74745071184c6d92f813cb15c13cae445af27.jpg",
                    "98c51036310577a32a488d075bc45103fc32b201.jpg"
                },
                new List<long> {
                    2274543,
                    2039184
                }
            }
        };

    [Theory]
    [MemberData(nameof(GetImageDownloadTestData))]
    public void ImageDownloadTest(List<string> imageIds, List<long> expectedSizes) {
        var data = HttpClient.Call(new MangaTokenRpc(imageIds))!.Data;

        var imageSizes = data.Select(l
            => HttpClient.GetByteArrayAsync($"{l.Url}?token={l.Token}").Result.Length).ToList();

        for (var i = 0; i < expectedSizes.Count; i++) {
            Assert.Equal(expectedSizes[i], imageSizes[i]);
        }
    }
}
