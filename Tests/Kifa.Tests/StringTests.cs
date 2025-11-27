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
        string.Or($"{null}").Should().Be(null);
        string.OrEmpty($"{null}").Should().Be("");
        var x = 100;
        string.Or($"{x}").Should().Be("100");
        string.OrEmpty($"{x}").Should().Be("100");
        int? y = null;
        string.Or($"{y}").Should().Be(null);
        string.OrEmpty($"{y}").Should().Be("");
        string.Or($"{x} {y}").Should().Be(null);
        string.OrEmpty($"{x} {y}").Should().Be("");

        string.Or($"{x} {y}", "c").Should().Be("c");
    }
}
