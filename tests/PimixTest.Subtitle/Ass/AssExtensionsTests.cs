using System;
using System.Drawing;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssExtensionsTests {
        [Fact]
        public void BoolTextTest() {
            Assert.Equal("-1", AssElementExtensions.ToString(true));
            Assert.Equal("0", AssElementExtensions.ToString(false));
        }

        [Fact]
        public void ColorTextTest() {
            Assert.Equal("&H3C141414", AssElementExtensions.ToString(Color.FromArgb(0x3C141414)));
        }

        [Fact]
        public void DoubleTextTest() {
            Assert.Equal("0.12", AssElementExtensions.ToString(0.123));
            Assert.Equal("12.12", AssElementExtensions.ToString(12.123));
            Assert.Equal("-0.12", AssElementExtensions.ToString((-0.123)));
            Assert.Equal("123.00", AssElementExtensions.ToString(123.0));
            Assert.Equal("0.12", AssElementExtensions.ToString(0.117));
        }

        [Fact]
        public void IntTextTest() {
            Assert.Equal("123", AssElementExtensions.ToString(123));
        }

        [Fact]
        public void StrTextTest() {
            Assert.Equal("abc", AssElementExtensions.ToString("abc"));
        }

        [Fact]
        public void TimeSpanTextTest() {
            Assert.Equal("1:06:41.23", AssElementExtensions.ToString(TimeSpan.FromSeconds(4001.234)));
        }
    }
}
