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
}
