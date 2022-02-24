using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliMediaTests {
    [Theory]
    [InlineData("md28222837", "深夜食堂 第三季", "电视剧", 28671)]
    [InlineData("md28231812", "关于我转生变成史莱姆这档事 第二季", "番剧", 36170)]
    [InlineData("md59632", "深夜食堂", "电影", 12075)]
    public void MediaRpcTest(string mediaId, string title, string typeName, long seasonId) {
        var result = new MediaRpc().Call(mediaId).Result.Media;
        Assert.Equal(title, result.Title);
        Assert.Equal(typeName, result.TypeName);
        Assert.Equal(seasonId, result.SeasonId);
    }

    [Theory]
    [InlineData("ss28671", "正片", 70710330, "第二十一话", "炸肉饼", "相关视频", 69501035, "预告1", "")]
    [InlineData("ss36170", "正片", 373619156, "24.9", "闲话：日向·坂口", "PV", 755557176, "PV1", "")]
    [InlineData("ss12075", "正片", 14681109, "正片", "")]
    public void MediaSeasonRpcTest(string seasonId, string title, long episode1Id,
        string episode1Title, string episode1LongTitle, string otherTitle = null,
        long otherEpisode1Id = 0, string otherEpisode1Title = null,
        string otherEpisode1LongTitle = null) {
        var result = new MediaSeasonRpc().Call(seasonId).Result;
        Assert.Equal(title, result.MainSection.Title);
        Assert.Equal(episode1Id, result.MainSection.Episodes.First().Aid);
        Assert.Equal(episode1Title, result.MainSection.Episodes.First().Title);
        Assert.Equal(episode1LongTitle, result.MainSection.Episodes.First().LongTitle);
        if (otherTitle != null) {
            var otherSection = result.Section.First();
            Assert.Equal(otherTitle, otherSection.Title);
            Assert.Equal(otherEpisode1Id, otherSection.Episodes.First().Aid);
            Assert.Equal(otherEpisode1Title, otherSection.Episodes.First().Title);
            Assert.Equal(otherEpisode1LongTitle, otherSection.Episodes.First().LongTitle);
        }
    }

    [Theory]
    [InlineData("md28222837", "深夜食堂 第三季", "电视剧", "av70710330", "av69501035")]
    [InlineData("md28231812", "关于我转生变成史莱姆这档事 第二季", "番剧", "av373619156", "av755557176")]
    [InlineData("md59632", "深夜食堂", "电影", "av14681109")]
    public void BangumiTest(string mediaId, string title, string typeName, string firstEpisodeAid,
        string firstExtraAid = null) {
        var bangumi = new BilibiliBangumi {
            Id = mediaId
        };
        bangumi.Fill();
        Assert.Equal(title, bangumi.Title);
        Assert.Equal(typeName, bangumi.Type);
        Assert.Equal(firstEpisodeAid, bangumi.Aids.First());
        if (firstExtraAid != null) {
            Assert.Equal(firstExtraAid, bangumi.ExtraAids.First());
        }
    }
}
