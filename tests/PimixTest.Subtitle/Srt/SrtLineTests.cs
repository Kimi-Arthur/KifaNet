using System;
using Pimix.Subtitle.Srt;
using Xunit;

namespace PimixTest.Subtitle.Srt {
    public class SrtLineTests {
        const string SrtLineText = "168\n" +
                                   "00:20:41,150 --> 00:20:45,109\n" +
                                   "- How did he do that?\n" +
                                   "- Made him an offer he couldn't refuse.";

        [Fact]
        public void ParseTest() {
            var line = SrtLine.Parse(SrtLineText);
            Assert.Equal(168, line.Index);
            Assert.Equal(new TimeSpan(0, 0, 20, 41, 150), line.StartTime);
            Assert.Equal(new TimeSpan(0, 0, 20, 45, 109), line.EndTime);
            Assert.Equal("- How did he do that?\n- Made him an offer he couldn't refuse.",
                line.Text.ToString());
        }

        [Fact]
        public void SerializeTest() {
            Assert.Equal(SrtLineText, SrtLine.Parse(SrtLineText).ToString());
        }
    }
}
