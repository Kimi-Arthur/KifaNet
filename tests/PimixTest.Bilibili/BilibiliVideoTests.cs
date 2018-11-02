using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Bilibili;
using Pimix.Service;

namespace PimixTest.Bilibili {
    [TestClass]
    public class BilibiliVideoTests {
        [TestMethod]
        public void SinglePartTest() {
            PimixService.PimixServerApiAddress = "http://www.pimix.tk/api";
            var video = PimixService.Get<BilibiliVideo>("av26361000");
            Assert.AreEqual("av26361000", video.Id);
            Assert.AreEqual("【7月】工作细胞 01【独家正版】", video.Title);
            Assert.AreEqual("#01", video.Description);
            Assert.IsTrue(video.Tags.SequenceEqual(new[] {"BILIBILI正版", "TV动画"}),
                "Keywords differ");
            Assert.AreEqual(1, video.Pages.Count());
            Assert.AreEqual("49053680", video.Pages.ElementAt(0).Cid);
            Assert.AreEqual("hatarakusaibou_ep01.mp4", video.Pages.ElementAt(0).Title);
            Assert.AreEqual(BilibiliVideo.PartModeType.SinglePartMode, video.PartMode);
            var doc = video.GenerateAssDocument();
            Assert.IsTrue(
                doc.ToString().StartsWith(
                    "[Script Info]\r\n" +
                    "Title: 【7月】工作细胞 01【独家正版】\r\n" +
                    "Original Script: Bilibili\r\n" +
                    "Script Type: V4.00+\r\n" +
                    "Collisions: Normal\r\n" +
                    "PlayResX: 1920\r\n" +
                    "PlayResY: 1080\r\n\r\n"));
        }

        [TestMethod]
        public void MultiPartsTest() {
            PimixService.PimixServerApiAddress = "http://www.pimix.tk/api";
            var video = PimixService.Get<BilibiliVideo>("av2044037");
            video.PartMode = BilibiliVideo.PartModeType.ContinuousPartMode;
            Assert.AreEqual("av2044037", video.Id);
            Assert.AreEqual("【日语学习】发音入门基础：50音图", video.Title);
            Assert.AreEqual("【封面爸爸去哪儿】\r\n日语发音基础详解，查漏补缺。", video.Description);
            Assert.IsTrue(video.Tags.SequenceEqual(
                    new[] {
                        "日语教程",
                        "日语学习",
                        "日语五十音图",
                        "学习日语的过程"
                    }),
                "Keywords differ");
            Assert.AreEqual(6, video.Pages.Count());
            var data = new List<Tuple<string, string>> {
                Tuple.Create(
                    "3164090",
                    "基础发音1：50音图あかさ"),
                Tuple.Create(
                    "3164091",
                    "基础发音2：50音图あかさ"),
                Tuple.Create(
                    "3164092",
                    "基础发音3：50音图た～ま"),
                Tuple.Create(
                    "3164093",
                    "基础发音4：50音图や～わ"),
                Tuple.Create(
                    "3164094",
                    "基础发音5：拗音，促音，拨音"),
                Tuple.Create(
                    "3164095",
                    "基础发音6：长音，アクセント")
            };

            var offset = TimeSpan.Zero;
            foreach (var item in data.Zip(video.Pages,
                (x, y) => Tuple.Create(x.Item1, x.Item2, y))) {
                Assert.AreEqual(item.Item1, item.Item3.Cid);
                Assert.AreEqual(item.Item2, item.Item3.Title);
                Assert.IsTrue(item.Item3.Comments.Count() > 1000, "Comments should be > 1000");
                Assert.AreEqual(offset, item.Item3.ChatOffset);
                offset += item.Item3.Duration;
            }

            video.PartMode = BilibiliVideo.PartModeType.ParallelPartMode;
            foreach (var item in video.Pages) {
                Assert.AreEqual(TimeSpan.Zero, item.ChatOffset);
            }
        }
    }
}
