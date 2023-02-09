using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Kifa.Subtitle.Ass;
using Kifa.Tencent.Rpcs;
using Xunit;

namespace Kifa.Tencent.Tests;

public class DanmuTests {
    HttpClient httpClient = new();

    [Fact]
    public void BaseDanmuRpcTest() {
        var response = httpClient.Call(new BaseDanmuRpc("i0045u918s5"));
        Assert.Equal(88, response.SegmentIndex.Count);
        Assert.Equal("t/v1/2100000/2130000", response.SegmentIndex["2100000"].SegmentName);
    }

    [Fact]
    public void SegmentDanmuRpcTest() {
        var response = httpClient.Call(new SegmentDanmuRpc("i0045u918s5", "t/v1/2100000/2130000"));
        Assert.Equal(600, response.BarrageList.Count);
        Assert.Equal("把心脏起搏器按停了",
            response.BarrageList.First(b => b.Id == "76561198061395137").Content);
        Assert.Equal(
            "{\"color\":\"ffffff\",\"gradient_colors\":[\"FF1964\",\"FF1964\"],\"position\":1}",
            response.BarrageList.First(b => b.Id == "76561198061432944").ContentStyle);
    }

    [Fact]
    public void ParseTest() {
        var danmuList = httpClient.Call(new SegmentDanmuRpc("i0045u918s5", "t/v1/210000/240000"))
            .BarrageList.Select(TencentDanmu.Parse).ToList();
        Assert.Equal(600, danmuList.Count);
        var danmu1 = danmuList.First(d => d.Text == "世界属于三体");
        Assert.Equal("世界属于三体", danmu1.Text);
        Assert.Equal(TimeSpan.FromSeconds(210), danmu1.VideoTime);
        Assert.Null(danmu1.TextColor);

        var danmu2 = danmuList.First(d => d.Text == "人类的命运，就在这纤细的两指之上");
        Assert.Equal(Color.FromArgb(0xCD, 0x87, 0xFF), danmu2.TextColor);
    }

    [Fact]
    public void GetDanmuListTest() {
        var danmuList = TencentVideo.GetDanmuList("i0045u918s5");
        danmuList.Should().HaveCountGreaterThan(52200);
    }

    [Fact]
    public void GenerateAssDialogTest() {
        var danmu1 = new TencentDanmu {
            Text = "世界属于三体",
            VideoTime = TimeSpan.FromSeconds(210),
        };

        danmu1.GenerateAssDialogue().Should().BeEquivalentTo(new AssDialogue {
            Start = TimeSpan.FromSeconds(210),
            End = TimeSpan.FromSeconds(218),
            Style = AssStyle.NormalCommentStyle,
            Text = new AssDialogueText {
                TextElements = new List<AssDialogueTextElement> {
                    new AssDialogueRawTextElement {
                        Content = "世界属于三体"
                    }
                }
            }
        });
        var danmu2 = new TencentDanmu {
            Text = "人类的命运，就在这纤细的两指之上",
            VideoTime = TimeSpan.FromSeconds(211),
            TextColor = Color.FromArgb(0xD0, 0xCD, 0x87, 0xFF)
        };

        danmu2.GenerateAssDialogue().Should().BeEquivalentTo(new AssDialogue {
            Start = TimeSpan.FromSeconds(211),
            End = TimeSpan.FromSeconds(219),
            Style = AssStyle.NormalCommentStyle,
            Text = new AssDialogueText {
                TextElements = new List<AssDialogueTextElement> {
                    new AssDialogueControlTextElement {
                        Elements = new List<AssControlElement> {
                            new PrimaryColourStyle {
                                Value = Color.FromArgb(0xD0, 0xCD, 0x87, 0xFF)
                            }
                        }
                    },
                    new AssDialogueRawTextElement {
                        Content = "人类的命运，就在这纤细的两指之上"
                    }
                }
            }
        });
    }
}
