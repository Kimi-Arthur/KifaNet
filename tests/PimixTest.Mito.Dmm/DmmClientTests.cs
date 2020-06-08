using Pimix.Mito;
using Pimix.Mito.Dmm;
using Xunit;

namespace PimixTest.Mito.Dmm {
    public class DmmClientTests {
        [Fact]
        public void GetVideoTest() {
            var client = new DmmClient();
            var video = new Video {Id = "SSNI-780"};
            client.Fill(video);
            Assert.Equal("SSNI-780", video.VideoIds.DmmId);
            Assert.Equal("ssni00780", video.VideoIds.DmmDvdId);
            Assert.Equal("三上悠亜", video.Actresses[0].Id);
            Assert.Equal("1030262", video.Actresses[0].Ids.DmmId);
            Assert.Equal("無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial 三上悠亜", video.Title);
            Assert.Equal(
                "ぷるんぷるんの巨乳！あの…もしかして、俺のこと誘ってる…！？常に無意識に男を誘惑する‘罪な巨乳’。人目をはばからず無防備に主張する、服を着てても抑えきれない、ハッキリ分かる豊満ボリューム。これって絶対誘ってるでしょ！男たちの視線を釘付けにする着衣巨乳の禁断誘惑！たわわに膨らみ、揺れ動き、透けて、食い込むおっぱいにもう我慢できません！日常生活に起こった奇跡の着衣巨乳フェチズム6シチュエーション！",
                video.Description);
        }

        [Fact]
        public void GetVideoWithMultipleActressesTest() {
            var client = new DmmClient();
            var video = new Video {Id = "SIVR-061"};
            client.Fill(video);
            Assert.Equal(4, video.Actresses.Count);
        }
    }
}
