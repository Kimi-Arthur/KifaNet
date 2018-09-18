using System;
using System.Drawing;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssStyleTests {
        [Fact]
        public void BasicTest() {
            var style = new AssStyle {
                Name = "Default",
                FontName = "simhei",
                FontSize = 28,
                PrimaryColour = Color.DarkOrange, // FFFF8C00
                SecondaryColour = Color.FromArgb(0x00000000),
                OutlineColour = Color.FromArgb(0x00111111),
                BackColour = Color.FromArgb(0x000D0D0D),
                Bold = true,
                Italic = false,
                Underline = false,
                StrikeOut = false,
                ScaleX = 100,
                ScaleY = 100,
                Spacing = 1,
                Angle = 0.00,
                BorderStyle = AssStyle.BorderStyleType.OutlineWithDropShadow,
                Outline = 2,
                Shadow = 0,
                Alignment = AssAlignment.BottomCenter,
                MarginL = 30,
                MarginR = 30,
                MarginV = 10,
                Encoding = 1
            };

            Assert.Equal(
                "Style: Default,simhei,28,&H00008CFF,&HFF000000,&HFF111111,&HFF0D0D0D,-1,0,0,0,100,100,1,0.00,1,2,0,2,30,30,10,1",
                style.ToString());
        }

        [Fact]
        public void OutlineArgumentRangeCheckTest() {
            var style = new AssStyle();
            for (var i = 0; i < 5; i++) {
                style.Outline = i;
            }

            foreach (var o in new[] {-1, 5, 123}) {
                Assert.Equal("Outline", Assert
                    .Throws<ArgumentOutOfRangeException>(() => style.Outline = o)
                    .ParamName);
            }
        }

        [Fact]
        public void ShadowArgumentRangeCheckTest() {
            var style = new AssStyle();
            for (var i = 0; i < 5; i++) {
                style.Shadow = i;
            }

            foreach (var s in new[] {-1, 5, 123}) {
                Assert.Equal("Shadow", Assert
                    .Throws<ArgumentOutOfRangeException>(() => style.Shadow = s)
                    .ParamName);
            }
        }

        [Fact]
        public void ValidNameTest() {
            var style = new AssStyle();
            style.Name = "staff";
            Assert.Equal("staff", style.ValidName);
            style.Name = "Default";
            Assert.Equal("*Default", style.ValidName);
        }
    }
}
