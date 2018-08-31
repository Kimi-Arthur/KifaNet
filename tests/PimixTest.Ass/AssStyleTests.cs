using System;
using System.Drawing;
using Pimix.Ass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PimixTest.Ass {
    [TestClass]
    public class AssStyleTests {
        [TestMethod]
        public void BasicTest() {
            AssElement style = new AssStyle() {
                Name = "Default",
                Fontname = "simhei",
                Fontsize = 28,
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

            Assert.AreEqual(
                "Style: Default,simhei,28,&HFF008CFF,&H00000000,&H00111111,&H000D0D0D,-1,0,0,0,100,100,1,0.00,1,2,0,2,30,30,10,1",
                style.GenerateAssText());
        }

        [TestMethod]
        public void OutlineArgumentRangeCheckTest() {
            var style = new AssStyle();
            for (int i = 0; i < 5; i++) {
                style.Outline = i;
            }

            foreach (var o in new[] {-1, 5, 123}) {
                Assert.AreEqual("Outline", Assert
                    .ThrowsException<ArgumentOutOfRangeException>(() => style.Outline = o)
                    .ParamName);
            }
        }

        [TestMethod]
        public void ShadowArgumentRangeCheckTest() {
            var style = new AssStyle();
            for (int i = 0; i < 5; i++) {
                style.Shadow = i;
            }

            foreach (var s in new[] {-1, 5, 123}) {
                Assert.AreEqual("Shadow", Assert
                    .ThrowsException<ArgumentOutOfRangeException>(() => style.Shadow = s)
                    .ParamName);
            }
        }

        [TestMethod]
        public void ValidNameTest() {
            var style = new AssStyle();
            style.Name = "staff";
            Assert.AreEqual("staff", style.ValidName);
            style.Name = "Default";
            Assert.AreEqual("*Default", style.ValidName);
        }
    }
}
