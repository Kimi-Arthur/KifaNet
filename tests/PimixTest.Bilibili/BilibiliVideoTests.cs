using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Bilibili;

namespace PimixTest.Bilibili {
    [TestClass]
    public class BilibiliVideoTests {
        [TestMethod]
        public void SinglePartTest() {
            var video = new BilibiliVideo("1858457");
            Assert.AreEqual("1858457", video.Id);
            Assert.AreEqual("Die Mannschaft", video.Title);
            Assert.AreEqual("ARD 一部关于德国队在2014年巴西世界杯夺冠旅程的纪录片。", video.Description);
            Assert.IsTrue(video.Tags.SequenceEqual(new[] {"德国队", "世界杯", "DFB"}),
                "Keywords differ");
            Assert.AreEqual(1, video.Pages.Count());
            Assert.AreEqual("2862733", video.Pages.ElementAt(0).Cid);
            Assert.AreEqual("", video.Pages.ElementAt(0).Title);
            Assert.AreEqual(BilibiliVideo.PartModeType.SinglePartMode, video.PartMode);
            var doc = video.GenerateAssDocument();
            Assert.AreEqual(
                "[Script Info]\r\nTitle: Die Mannschaft\r\nOriginal Script: Bilibili\r\nScript Type: V4.00+\r\n",
                doc.GenerateAssText());
        }

        [TestMethod]
        public void MultiPartsTest() {
            var video = new BilibiliVideo("1852616");
            Assert.AreEqual("1852616", video.Id);
            Assert.AreEqual("【NHK红白歌合战】141231 第65回 全场高清中文字幕【东京不够热字幕组】", video.Title);
            Assert.AreEqual(
                "第65回 NHK红白歌合战全场\r 最后一个年末音番！至此，东热字幕组的2014年末音番制作结束，奋斗了一个多月，大家辛苦了！\r 第一弹：Best Hits歌谣祭2014全场→av1737581 第二弹：Best Artist 2014全场→av1753285 第三弹：FNS歌谣祭2014全场→av1778707 第四弹：Music Station Super Live 2014全场→av1839990",
                video.Description);
            Assert.IsTrue(video.Tags.SequenceEqual(
                    new[] {
                        "NHK红白歌合战",
                        "红白歌会",
                        "141231",
                        "AKB48",
                        "南天群星",
                        "福山雅治",
                        "SMAP",
                        "吉高由里子",
                        "TOKIO"
                    }),
                "Keywords differ");
            Assert.AreEqual(5, video.Pages.Count());
            var data = new List<Tuple<string, string>> {
                Tuple.Create(
                    "2852771",
                    "1、HKT48|Sexy Zone|E-girls|AAA|妖表|miwa|福田こうへい|SKE48|NMB48|郷ひろみ|藤あや子|色涂"),
                Tuple.Create(
                    "2852772",
                    "2、西川x水树|Chris Hart|伍代夏子|三代目JSB|西野加奈|香西かおり|細川たかし|德永英明|天童よしみ|岚x妖表|坂本冬美|森進一|和田アキ子|V6"),
                Tuple.Create(
                    "2852773",
                    "3、花子与安妮SP|絢香|May J.|世终|Perfume|金爆|桃草|关8|何炅|水森かおり|五木ひろし"),
                Tuple.Create(
                    "2852774",
                    "4、生物股长|彭薇薇|TOKIO|冰雪奇缘SP|SMAP|椎名林檎|EXILE|薬師丸ひろ子|石川さゆり|長渕剛|中森明菜"),
                Tuple.Create(
                    "2852775",
                    "5、AKB48|福山雅治|中島みゆき|美輪明宏|南天群星|岚|松田聖子|合唱故乡")
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
