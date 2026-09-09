using System.Collections.Generic;
using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliArchiveTests {
    public BilibiliArchiveTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void RpcTest() {
        HttpClients.GetBilibiliClient()
            .Call(new ArchiveRpc(uploaderId: "43536", seasonId: "1808473")).Data.Checked().Aids
            .Should().HaveCountGreaterOrEqualTo(6);
    }

    [Fact]
    public void FillTest() {
        var archive = new BilibiliArchive {
            Id = "43536/820817"
        };
        archive.Fill();

        Assert.Equal("合集·【艾尔登法环】全流程地毯式超详细黑桐谷歌游戏视频解说", archive.Title);
        Assert.Equal("黑桐谷歌", archive.Author);
        archive.Videos.Should().HaveCountGreaterOrEqualTo(52);
    }

    [Fact]
    public void GetArchiveFolderTest() {
        var archive = new BilibiliArchive {
            Id = "3494353567746383/8989314",
            SeasonId = "8989314",
            Title = "合集·Season/Name / Part 1"
        };

        var video = new BilibiliVideo {
            Id = "av170001",
            Title = "【MV】保加利亚妖王AZIS视频合辑",
            Author = "冰封.虾子",
            AuthorId = "122541",
            Pages = new List<BilibiliChat> {
                new() {
                    Id = 1,
                    Cid = "279786",
                    Title = "Хоп"
                }
            }
        };

        var desiredName = video.GetDesiredName(1, 64, 0, includePageTitle: false,
            extraFolder: archive.GetArchiveFolder(), prefix: "2020-01-01");

        desiredName.Should().Be(
            "冰封.虾子.122541.bilibili/Season／Name ／ Part 1.8989314/2020-01-01 【MV】保加利亚妖王AZIS视频合辑.av170001p1.c279786.64");
    }
}
