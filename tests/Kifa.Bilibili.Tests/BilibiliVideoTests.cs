using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Bilibili.BiliplusApi;
using Kifa.Configs;
using Kifa.Service;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliVideoTests {
    public BilibiliVideoTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void BiliplusVideoCacheRpcTest() {
        var data = new BiliplusVideoCacheRpc().Invoke("av170001").Data;
        Assert.Equal(5, data.Parts.Count);
        Assert.Equal("Хоп", data.Parts[0].Part);
    }

    [Fact]
    public void BiliplusVideoRpcTest() {
        var data = new BiliplusVideoRpc().Invoke("av170001");
        Assert.Equal(10, data.List.Count);
        Assert.Equal("Хоп", data.List[0].Part);
    }

    [Fact]
    public void BilibiliVideoRpcTest() {
        var data = BilibiliVideo.GetBilibiliClient().Call(new VideoRpc("av170001")).Data;
        Assert.Equal(10, data.Pages.Count);
        Assert.Equal("Хоп", data.Pages[0].Part);
    }

    [Fact]
    public void BilibiliVideoTagRpcTest() {
        var data = BilibiliVideo.GetBilibiliClient().Call(new VideoTagRpc("av170001")).Data;
        Assert.Equal(5, data.Count);
        Assert.Equal("保加利亚妖王", data[0].TagName);
    }

    [Theory]
    [InlineData("av170001", "【MV】保加利亚妖王AZIS视频合辑", "2011-11-09 22:55:33.000000", "保加利亚妖王")]
    [InlineData("av1757900", "【李狗嗨】可爱的雅人叔", "2014-11-29 18:19:00.000000", "LEGAL")]
    [InlineData("av27001", null, "av27001", null)]
    public void FillTest(string id, string title, string dateString, string? tag) {
        var video = new BilibiliVideo {
            Id = id
        };
        video.Fill();
        Assert.Equal(title, video.Title);
        Assert.Contains(dateString, video.ToString());
        if (tag != null) {
            Assert.Contains(tag, video.Tags);
        }
    }

    [Fact]
    public void SinglePartTest() {
        var video = BilibiliVideo.Client.Get("av26361000");
        Assert.Equal("av26361000", video.Id);
        Assert.Equal("【7月】工作细胞 01【独家正版】", video.Title);
        Assert.Equal("#01", video.Description);
        Assert.Equal(new[] { "BILIBILI正版", "TV动画" }, video.Tags.Take(2));
        Assert.Single(video.Pages);
        Assert.Equal("49053680", video.Pages.ElementAt(0).Cid);
        Assert.Equal("hatarakusaibou_ep01.mp4", video.Pages.ElementAt(0).Title);
        Assert.Equal(BilibiliVideo.PartModeType.SinglePartMode, video.PartMode);
        var doc = video.GenerateAssDocument();
        Assert.StartsWith(
            "[Script Info]\n" + "Title: 【7月】工作细胞 01【独家正版】\n" + "Original Script: Bilibili\n" +
            "Script Type: V4.00+\n" + "Collisions: Normal\n", doc.ToString());
    }

    [Fact]
    public void MultiPartsTest() {
        var video = BilibiliVideo.Client.Get("av2044037");
        video.PartMode = BilibiliVideo.PartModeType.ContinuousPartMode;
        Assert.Equal("av2044037", video.Id);
        Assert.Equal("【日语学习】发音入门基础：50音图", video.Title);
        Assert.Equal("【封面爸爸去哪儿】\r\n日语发音基础详解，查漏补缺。", video.Description);
        Assert.Equal(new[] { "学习日语的过程", "日语五十音图", "日语学习", "日语教程" }, video.Tags);
        Assert.Equal(6, video.Pages.Count());
        var data = new List<Tuple<string, string>> {
            Tuple.Create("3164090", "基础发音1：50音图あかさ"),
            Tuple.Create("3164091", "基础发音2：50音图あかさ"),
            Tuple.Create("3164092", "基础发音3：50音图た～ま"),
            Tuple.Create("3164093", "基础发音4：50音图や～わ"),
            Tuple.Create("3164094", "基础发音5：拗音，促音，拨音"),
            Tuple.Create("3164095", "基础发音6：长音，アクセント")
        };

        var offset = TimeSpan.Zero;
        foreach (var item in data.Zip(video.Pages, (x, y) => Tuple.Create(x.Item1, x.Item2, y))) {
            Assert.Equal(item.Item1, item.Item3.Cid);
            Assert.Equal(item.Item2, item.Item3.Title);
            Assert.True(item.Item3.Comments.Count() > 1000, "Comments should be > 1000");
            Assert.Equal(offset, item.Item3.ChatOffset);
            offset += item.Item3.Duration;
        }

        video.PartMode = BilibiliVideo.PartModeType.ParallelPartMode;
        foreach (var item in video.Pages) {
            Assert.Equal(TimeSpan.Zero, item.ChatOffset);
        }
    }

    [Theory]
    [InlineData("/Downloads/bilibili/$/c279786.64.mp4", null, 0, null, 0)]
    [InlineData("/Downloads/bilibili/$/av170001p1.c279786.64.mp4", "av170001", 1, "279786", 64)]
    [InlineData("/Downloads/bilibili/冰封.虾子-122541/【MV】保加利亚妖王AZIS视频合辑.av170001p1.c279786.64.mp4",
        "av170001", 1, "279786", 64)]
    [InlineData("/Downloads/bilibili/冰封.虾子-122541/【MV】保加利亚妖王AZIS视频合辑-av170001p2.c279786.64.mp4",
        "av170001", 2, "275431", 64)]
    [InlineData("/Downloads/bilibili/冰封.虾子-122541/【MV】保加利亚妖王AZIS视频合辑.av170001.c279786.64.mp4",
        "av170001", 1, "279786", 64)]
    public void VideoFileNameParsing(string fileName, string? aid, int pid, string cid,
        int quality) {
        var result = BilibiliVideo.Parse(fileName);
        if (aid == null) {
            Assert.Null(result.video);
            return;
        }

        Assert.Equal(aid, result.video.Id);
        Assert.Equal(pid, result.pid);
        Assert.Equal(cid, result.video.Pages[pid - 1].Cid);
        Assert.Equal(quality, result.quality);
    }
}
