using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Xunit;

namespace Kifa.Bilibili.Tests {
    public class BilibiliUploaderTests {
        [Fact]
        public void VideoRpcTest() {
            Assert.True(new UploaderVideoRpc().Call("43536").Data.List.Vlist.Count > 200);
        }

        [Fact]
        public void InfoRpcTest() {
            Assert.Equal("黑桐谷歌", new UploaderInfoRpc().Call("43536").Data.Name);
        }

        [Fact]
        public void FillTest() {
            var uploader = new BilibiliUploader {Id = "43536"};
            uploader.Fill();
            Assert.Equal("黑桐谷歌", uploader.Name);
            Assert.True(uploader.Aids.Count > 200);
            Assert.Equal("av27700", uploader.Aids.First());
        }
    }
}
