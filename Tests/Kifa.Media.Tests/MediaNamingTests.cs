using System;
using System.IO;
using FluentAssertions;
using Kifa.Media;
using Xunit;

namespace Kifa.Media.Tests;

public class MediaNamingTests {
    [Theory]
    [InlineData("Apple", "iPhone 15 Pro Max", "ip15pm")]
    [InlineData("Apple", "iPhone 15 Pro", "ip15p")]
    [InlineData("Apple", "iPhone 16", "ip16")]
    [InlineData("Apple", "iPhone SE", "ipse")]
    [InlineData("SONY", "ILCE-7M4", "a7m4")]
    [InlineData("Sony", "ILCE-7RM5", "a7rm5")]
    [InlineData("Canon", "Canon EOS R5", "r5")]
    [InlineData("Canon", "Canon EOS R6 Mark II", "r6m2")]
    [InlineData("Google", "Pixel 8 Pro", "p8p")]
    [InlineData("Google", "Pixel 9 Pro XL", "p9pxl")]
    [InlineData("DJI", "FC3582", "mini4p")]
    [InlineData("FUJIFILM", "X-T5", "xt5")]
    public void ProposeDeviceTagTest(string make, string model, string expected) {
        MediaTagResolver.ProposeDeviceTag(make, model).Should().Be(expected);
    }

    [Theory]
    [InlineData("com.tencent.mm", "wechat")]
    [InlineData("com.sina.weibo", "weibo")]
    [InlineData("tv.danmaku.bili", "bilibili")]
    [InlineData("com.ss.android.ugc.aweme", "douyin")]
    [InlineData("com.android.chrome", "chrome")]
    [InlineData("com.twitter.android", "twitter")]
    [InlineData("com.unknown.foobar", "foobar")]
    public void ProposeAppTagTest(string package, string expected) {
        MediaTagResolver.ProposeAppTag(package).Should().Be(expected);
    }

    [Fact]
    public void ExtractFromAndroidScreenshotFileNameTest() {
        var fileName = "Screenshot_2022-01-10-10-54-08-331_com.tencent.mm.jpg";
        using var stream = new MemoryStream();
        var metadata = MediaMetadataService.Extract(stream, fileName);

        metadata.IsScreenshot.Should().BeTrue();
        metadata.CapturedAt.Should().Be(new DateTime(2022, 1, 10, 10, 54, 8));
        metadata.SubSecond.Should().Be("331");
        metadata.AppPackage.Should().Be("com.tencent.mm");

        var sourceTag = MediaTagResolver.ResolveSourceTag(metadata);
        sourceTag.Should().Be("wechat");

        metadata.FormatBaseName(sourceTag).Should().Be("20220110_105408.331_wechat");
    }

    [Fact]
    public void ExtractFromMacScreenshotFileNameTest() {
        var fileName = "Screenshot 2022-01-10 at 10.54.08.png";
        using var stream = new MemoryStream();
        var metadata = MediaMetadataService.Extract(stream, fileName);

        metadata.IsScreenshot.Should().BeTrue();
        metadata.CapturedAt.Should().Be(new DateTime(2022, 1, 10, 10, 54, 8));
        metadata.SubSecond.Should().BeNull();
        metadata.AppPackage.Should().BeNull();

        var sourceTag = MediaTagResolver.ResolveSourceTag(metadata);
        sourceTag.Should().Be("shot");

        metadata.FormatBaseName(sourceTag).Should().Be("20220110_105408_shot");
    }

    [Fact]
    public void FormatBaseNameVariantsTest() {
        var date = new DateTime(2026, 9, 2, 14, 30, 55);

        // Full metadata with subsec + device
        var meta1 = new MediaMetadata {
            CapturedAt = date,
            SubSecond = "842"
        };
        meta1.FormatBaseName("ip15p").Should().Be("20260902_143055.842_ip15p");

        // Metadata without subsec
        var meta2 = new MediaMetadata {
            CapturedAt = date
        };
        meta2.FormatBaseName("a7m4").Should().Be("20260902_143055_a7m4");

        // With collision sequence
        meta1.FormatBaseName("ip15p", 1).Should().Be("20260902_143055.842_ip15p_1");

        // Generic image without device / app
        meta2.FormatBaseName(null).Should().Be("20260902_143055");
    }
}
