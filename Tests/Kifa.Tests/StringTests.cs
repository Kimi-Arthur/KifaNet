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
}
