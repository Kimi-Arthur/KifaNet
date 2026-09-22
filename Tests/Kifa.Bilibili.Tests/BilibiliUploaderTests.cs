using System;
using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliUploaderTests {
    public BilibiliUploaderTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void UploaderVideoRpcTest() {
        HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc("43536")).Data.Items.Should()
            .HaveCountGreaterThan(10);
    }

    [Fact]
    public void UploaderInfoRpcTest() {
        Assert.Equal("黑桐谷歌",
            HttpClients.GetBilibiliClient().Call(new UploaderInfoWebRpc("43536")).Space.Info.Name);
    }

    [Fact]
    public void UploaderInfoWebRpcDeserializationTest() {
        var json = """
        {
            "common": {
                "userInfo": {
                    "isLogin": false,
                    "face": "",
                    "vipStatus": 0,
                    "vipType": 0,
                    "completed": false
                },
                "serverConfig": {
                    "constants": {
                        "newClipboard": false
                    },
                    "openappDialogConfig": {
                        "commonOpenappFailedLimit": 3,
                        "autoOpenappLimit": 3
                    }
                },
                "abtest": {
                    "h5_resolution": "0"
                }
            },
            "space": {
                "mid": 3546568921713151,
                "info": {
                    "mid": 3546568921713151,
                    "name": "Golaniyule0的狗",
                    "school": {
                        "name": "Zhejiang University"
                    },
                    "vip": {
                        "ott_info": {
                            "vip_type": 0,
                            "pay_type": 0,
                            "pay_channel_id": "",
                            "status": 0,
                            "overdue_time": 0
                        },
                        "super_vip": {
                            "is_super_vip": false
                        }
                    }
                }
            },
            "video": {
                "quality": 16,
                "streamQuality": 16,
                "archiveType": 0
            },
            "route": {
                "params": {
                    "id": "3546568921713151"
                }
            }
        }
        """;

        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<UploaderInfoWebRpc.Response>(json, KifaJsonSerializerSettings.Default);
        response.Should().NotBeNull();
        response!.Common.Should().NotBeNull();
        response.Common.Abtest.Should().NotBeNull();
        response.Common.Abtest.H5Resolution.Should().Be("0");
        response.Space.Info.Name.Should().Be("Golaniyule0的狗");
        response.Space.Mid.Should().Be(3546568921713151L);
        response.Space.Info.School.Name.Should().Be("Zhejiang University");
        response.Video.Quality.Should().Be(16);
        response.Route.Params.Id.Should().Be("3546568921713151");
        response.Common.UserInfo.Completed.Should().BeFalse();
    }

    [Fact]
    public void UploaderModelTest() {
        var uploader = new BilibiliUploader {
            Id = "18427691",
            Name = "壹壹yeamusic"
        };
        uploader.Id.Should().Be("18427691");
        uploader.Name.Should().Be("壹壹yeamusic");
        uploader.RefreshInterval.Should().Be(TimeSpan.FromDays(30));
        uploader.GetUploaderFolder().NormalizeFilePath()
            .Should().Be("壹壹yeamusic.18427691.bilibili");
    }

    [Fact]
    public void UploaderVideosModelTest() {
        var uploaderVideos = new BilibiliUploaderVideos {
            Id = "18427691",
            Aids = ["av100", "av200"],
            RemovedAids = ["av50"]
        };
        uploaderVideos.Id.Should().Be("18427691");
        uploaderVideos.Aids.Should().Equal("av100", "av200");
        uploaderVideos.RemovedAids.Should().Equal("av50");
        uploaderVideos.RefreshInterval.Should().Be(TimeSpan.FromDays(1));
    }

    [Fact]
    public void UploaderFillTest() {
        var uploader = new BilibiliUploader {
            Id = "18427691"
        };
        uploader.Fill();
        uploader.Name.Should().Be("壹壹yeamusic");
        uploader.RefreshInterval.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public void UploaderVideosFillTest() {
        var uploaderVideos = new BilibiliUploaderVideos {
            Id = "18427691"
        };
        uploaderVideos.Fill();
        uploaderVideos.Aids[^1].Should().Be("av561513930");
        uploaderVideos.Aids.Should().HaveCount(104);
        uploaderVideos.RefreshInterval.Should().Be(TimeSpan.FromDays(1));
    }

    [Fact]
    public void UploaderVideosDeepFillTest() {
        var uploaderVideos = new BilibiliUploaderVideos {
            Id = "18427691"
        };
        uploaderVideos.Fill(deep: true);
        uploaderVideos.Aids[^1].Should().Be("av561513930");
        uploaderVideos.Aids.Should().HaveCount(104);
        uploaderVideos.RefreshInterval.Should().Be(TimeSpan.FromDays(1));
    }
}
