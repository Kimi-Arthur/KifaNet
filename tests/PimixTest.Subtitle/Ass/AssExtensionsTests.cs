using System;
using System.Drawing;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssExtensionsTests {
        [Fact]
        public void BoolTextTest() {
            Assert.Equal("-1", true.GenerateAssText());
            Assert.Equal("0", false.GenerateAssText());
        }

        [Fact]
        public void ColorTextTest() {
            Assert.Equal("&H3C141414", Color.FromArgb(0x3C141414).GenerateAssText());
        }

        [Fact]
        public void DoubleTextTest() {
            Assert.Equal("0.12", 0.123.GenerateAssText());
            Assert.Equal("12.12", 12.123.GenerateAssText());
            Assert.Equal("-0.12", (-0.123).GenerateAssText());
            Assert.Equal("123.00", 123.0.GenerateAssText());
            Assert.Equal("0.12", 0.117.GenerateAssText());
        }

        [Fact]
        public void IntTextTest() {
            Assert.Equal("123", 123.GenerateAssText());
        }

        [Fact]
        public void StrTextTest() {
            Assert.Equal("abc", "abc".GenerateAssText());
        }

        [Fact]
        public void TimeSpanTextTest() {
            Assert.Equal("1:06:41.23", TimeSpan.FromSeconds(4001.234).GenerateAssText());
        }
    }
}
