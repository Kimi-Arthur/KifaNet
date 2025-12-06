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
}
