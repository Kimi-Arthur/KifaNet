using System;
using Kifa.Subtitle.Srt;
using Xunit;

namespace Kifa.Subtitle.Tests.Srt;

public class SrtLineTests {
    const string SrtLineText = "168\n" + "00:20:41,150 --> 00:20:45,109\n" +
                               "- How did he do that?\n" +
                               "- Made him an offer he couldn't refuse.";

    [Theory]
    [InlineData(SrtLineText)]
    [InlineData(SrtLineText + "\n")]
    [InlineData(SrtLineText + "\n\n")]
    public void ParseTest(string content) {
        var line = SrtLine.Parse(content);
        Assert.Equal(168, line.Index);
        Assert.Equal(new TimeSpan(0, 0, 20, 41, 150), line.StartTime);
        Assert.Equal(new TimeSpan(0, 0, 20, 45, 109), line.EndTime);
        Assert.Equal("- How did he do that?\n- Made him an offer he couldn't refuse.",
            line.Text.ToString());
    }

    [Theory]
    [InlineData(SrtLineText)]
    [InlineData(SrtLineText + "\n")]
    [InlineData(SrtLineText + "\n\n")]
    public void SerializeTest(string content) {
        Assert.Equal(SrtLineText, SrtLine.Parse(content).ToString());
    }
}
