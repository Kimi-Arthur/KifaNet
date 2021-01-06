using Pimix.Bilibili.BilibiliApi;
using Xunit;

namespace Kifa.Bilibili.Tests {
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
    }
}
