using Pimix.Bilibili.BilibiliApi;
using Xunit;

namespace KifaTest.Bilibili {
    public class BilibiliUploaderTests {
        [Fact]
        public void RpcTest() {
            Assert.True(new UploaderRpc().Call("501271968").Data.List.Vlist.Count > 50);
        }
    }
}
