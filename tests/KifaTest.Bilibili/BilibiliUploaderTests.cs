using Pimix.Bilibili.BilibiliApi;
using Xunit;

namespace KifaTest.Bilibili {
    public class BilibiliUploaderTests {
        [Fact]
        public void RpcTest() {
            Assert.True(new UploaderRpc().Call("43536").Data.List.Vlist.Count > 200);
        }
    }
}
