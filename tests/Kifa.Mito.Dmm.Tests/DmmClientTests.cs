using System.Collections.Generic;
using Xunit;

namespace Kifa.Mito.Dmm.Tests {
    public class DmmClientTests {
        [Fact]
        public void GetVideoTest() {
            var client = new DmmClient();
            var video = new Video {Id = "SSNI-780"};
            client.Fill(video);
            Assert.Equal("SSNI-780", video.VideoIds.DmmId);
            Assert.Equal("ssni00780", video.VideoIds.DmmDvdId);
            Assert.Equal("三上悠亜", video.Actresses[0].Id);
            Assert.Equal("三上悠亜", video.Actresses[0].Name);
            Assert.Equal("1030262", video.Actresses[0].Ids.DmmId);
            Assert.Equal("無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial", video.Title);
            Assert.Equal(
                "ぷるんぷるんの巨乳！あの…もしかして、俺のこと誘ってる…！？常に無意識に男を誘惑する‘罪な巨乳’。人目をはばからず無防備に主張する、服を着てても抑えきれない、ハッキリ分かる豊満ボリューム。" +
                "これって絶対誘ってるでしょ！男たちの視線を釘付けにする着衣巨乳の禁断誘惑！たわわに膨らみ、揺れ動き、透けて、食い込むおっぱいにもう我慢できません！日常生活に起こった奇跡の着衣巨乳フェチズム6シチュエーション！",
                video.Description);
            Assert.Equal("/Venus/JAV/SSNI-780", video.Path);
            Assert.Equal(
                new List<string> {"/Venus/JAV/#Actress/三上悠亜/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial"},
                video.PathsByActress);
            Assert.Equal(
                new List<string> {
                    "/Venus/JAV/#Category/ハイビジョン/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial",
                    "/Venus/JAV/#Category/独占配信/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial",
                    "/Venus/JAV/#Category/巨乳/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial",
                    "/Venus/JAV/#Category/巨乳フェチ/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial",
                    "/Venus/JAV/#Category/ギリモザ/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial",
                    "/Venus/JAV/#Category/アイドル・芸能人/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial",
                    "/Venus/JAV/#Category/単体作品/SSNI-780 無意識に男を挑発する着衣巨乳 超ラッキースケベ妄想シチュエーションSpecial"
                }, video.PathsByCategory);
        }

        [Fact]
        public void GetSivrVideo() {
            var client = new DmmClient();
            var video = new Video {Id = "SIVR-061"};
            client.Fill(video);
            Assert.Equal(4, video.Actresses.Count);
            Assert.Equal("/Venus/JAV VR/SIVR-061", video.Path);
            Assert.Equal(
                new List<string> {
                    "/Venus/JAV VR/#Actress/三上悠亜/SIVR-061 【VR】S1ドリ-ム共演VR 史上最高の密着フォーメーションASMR 超高級4Pソープご奉仕Special",
                    "/Venus/JAV VR/#Actress/葵つかさ/SIVR-061 【VR】S1ドリ-ム共演VR 史上最高の密着フォーメーションASMR 超高級4Pソープご奉仕Special",
                    "/Venus/JAV VR/#Actress/筧ジュン/SIVR-061 【VR】S1ドリ-ム共演VR 史上最高の密着フォーメーションASMR 超高級4Pソープご奉仕Special",
                    "/Venus/JAV VR/#Actress/ひなたまりん/SIVR-061 【VR】S1ドリ-ム共演VR 史上最高の密着フォーメーションASMR 超高級4Pソープご奉仕Special",
                }, video.PathsByActress);
        }
    }
}
