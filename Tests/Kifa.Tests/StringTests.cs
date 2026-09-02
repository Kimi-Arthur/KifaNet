using FluentAssertions;
using Xunit;

namespace Kifa.Tests;

public class StringTests {
    [Theory]
    [InlineData(1024, "1.0KB")]
    [InlineData(1023, "1023B")]
    [InlineData(1025, "1.0KB")]
    [InlineData((1 << 30) + (1 << 28), "1.3GB")]
    public void ToSizeStringTest(long size, string sizeString) {
        Assert.Equal(sizeString, size.ToSizeString());
    }

    [Fact]
    public void OrNullTest() {
        string.FormatOr($"{null}").Should().Be(null);
        string.FormatOrEmpty($"{null}").Should().Be("");
        var x = 100;
        string.FormatOr($"{x}").Should().Be("100");
        string.FormatOrEmpty($"{x}").Should().Be("100");
        int? y = null;
        string.FormatOr($"{y}").Should().Be(null);
        string.FormatOrEmpty($"{y}").Should().Be("");
        string.FormatOr($"{x} {y}").Should().Be(null);
        string.FormatOrEmpty($"{x} {y}").Should().Be("");

        string.FormatOr($"{x} {y}", "c").Should().Be("c");
    }

    [Fact]
    public void ChopEndToByteCountTest() {
        "hello".ChopEndToByteCount(10).Should().Be("hello");
        "hello".ChopEndToByteCount(5).Should().Be("hello");
        "hello".ChopEndToByteCount(4).Should().Be("hel+");
        "hello".ChopEndToByteCount(3).Should().Be("he+");
        "hello".ChopEndToByteCount(2).Should().Be("h+");
        "hello".ChopEndToByteCount(1).Should().Be("+");

        // Multibyte CJK (3 bytes each)
        "你好世界".ChopEndToByteCount(12).Should().Be("你好世界");
        "你好世界".ChopEndToByteCount(11).Should().Be("你好世+"); // 3+3+3+1 = 10 bytes <= 11
        "你好世界".ChopEndToByteCount(10).Should().Be("你好世+"); // 3+3+3+1 = 10 bytes <= 10
        "你好世界".ChopEndToByteCount(9).Should().Be("你好+"); // 3+3+1 = 7 bytes <= 9
        "你好世界".ChopEndToByteCount(7).Should().Be("你好+"); // 3+3+1 = 7 bytes <= 7
        "你好世界".ChopEndToByteCount(6).Should().Be("你+"); // 3+1 = 4 bytes <= 6

        // Surrogate pairs / Emojis (4 bytes UTF-8, 2 chars in UTF-16)
        "😀😁😂".ChopEndToByteCount(12).Should().Be("😀😁😂");
        "😀😁😂".ChopEndToByteCount(10).Should().Be("😀😁+"); // 4+4+1 = 9 bytes <= 10
        "😀😁😂".ChopEndToByteCount(5).Should().Be("😀+"); // 4+1 = 5 bytes <= 5
        "😀😁😂".ChopEndToByteCount(4).Should().Be("+"); // 1 byte <= 4

        var actZero = () => "hello".ChopEndToByteCount(0);
        actZero.Should().Throw<System.ArgumentException>();
        var actNegative = () => "hello".ChopEndToByteCount(-2);
        actNegative.Should().NotThrow(); // negative means no limit
    }

    [Fact]
    public void NormalizeFileNameTest() {
        "  a/b\\c: d|e?f*g<h>i\nj  ".NormalizeFileName()
            .Should().Be("a／b＼c：d｜e？f＊g＜h＞i j");

        // Default max byte count is 250.
        var valid250 = new string('a', 250);
        valid250.NormalizeFileName().Should().Be(valid250);

        var overlong251 = new string('a', 251);
        overlong251.NormalizeFileName().Should().Be(new string('a', 249) + "+");

        // Custom maxFileNameByteCount
        "hello world".NormalizeFileName(maxFileNameByteCount: 6).Should().Be("hello+");

        // With suffix
        "hello".NormalizeFileName(".mp4", 10).Should().Be("hello.mp4");
        "hello world".NormalizeFileName(".mp4", 9).Should().Be("hell+.mp4");
        "hello world".NormalizeFileName(".mp4", 10).Should().Be("hello+.mp4");

        // Multibyte with suffix
        "你好世界".NormalizeFileName(".mp4", 10).Should().Be("你+.mp4");

        // Overlong suffix
        var actOverlongSuffix = () => "hello".NormalizeFileName("overlong_suffix", 5);
        actOverlongSuffix.Should().Throw<System.ArgumentException>();

        // Negative limit means no chopping
        "hello world".NormalizeFileName(".mp4", -1).Should().Be("hello world.mp4");
    }

    [Fact]
    public void NormalizeFilePathTest() {
        // Single file segment follows 250 limit
        var valid250 = new string('a', 250);
        valid250.NormalizeFilePath().Should().Be(valid250);

        var overlong251 = new string('a', 251);
        overlong251.NormalizeFilePath().Should().Be(new string('a', 249) + "+");

        // Multi-segment: directory follows 255 limit, file follows 250 limit
        var dir255 = new string('d', 255);
        var dir256 = new string('d', 256);
        $"{dir255}/{valid250}".NormalizeFilePath().Should().Be($"{dir255}/{valid250}");
        $"{dir256}/{overlong251}".NormalizeFilePath()
            .Should().Be($"{new string('d', 254)}+/{new string('a', 249)}+");

        // Named parameter limits
        "folder/hello world".NormalizeFilePath(maxFileNameByteCount: 6).Should().Be("folder/hello+");
        "folder_long/hello world".NormalizeFilePath(maxFileNameByteCount: 6, maxPathSegmentByteCount: 8)
            .Should().Be("folder_+/hello+");

        // With suffix on final file segment
        "folder/hello world".NormalizeFilePath(suffix: ".mp4", maxFileNameByteCount: 10,
            maxPathSegmentByteCount: 15).Should().Be("folder/hello+.mp4");

        // Safe characters mapping preserved across path segments
        "dir: 1/file? 2".NormalizeFilePath().Should().Be("dir：1/file？ 2");
    }
}
