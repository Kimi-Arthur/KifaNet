using System.Collections.Generic;
using System.Drawing;
using Pimix.Subtitle.Ass;
using Xunit;

namespace PimixTest.Subtitle.Ass {
    public class AssStylesSectionTests {
        [Fact]
        public void BasicTest() {
            var stylesSection = new AssStylesSection {
                Styles = new List<AssStyle> {
                    new AssStyle {
                        Name = "Default",
                        FontName = "simhei",
                        FontSize = 28,
                        PrimaryColour = Color.FromArgb(0x00FFFFFF),
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
                    },
                    new AssStyle {
                        Name = "staff",
                        FontName = "youyuan",
                        FontSize = 26,
                        PrimaryColour = Color.FromArgb(0x00EBEBEB),
                        SecondaryColour = Color.FromArgb(0x00000000),
                        OutlineColour = Color.FromArgb(0x28000000),
                        BackColour = Color.FromArgb(0x00000000),
                        Bold = true,
                        Italic = false,
                        Underline = false,
                        StrikeOut = false,
                        ScaleX = 100,
                        ScaleY = 100,
                        Spacing = 0,
                        Angle = 0.00,
                        BorderStyle = AssStyle.BorderStyleType.OutlineWithDropShadow,
                        Outline = 0,
                        Shadow = 2,
                        Alignment = AssAlignment.BottomCenter,
                        MarginL = 15,
                        MarginR = 15,
                        MarginV = 10,
                        Encoding = 1
                    }
                }
            };

            Assert.Equal("[V4+ Styles]\n"
                         + "Format: Name,Fontname,Fontsize,PrimaryColour,SecondaryColour,OutlineColour,BackColour,Bold,Italic,Underline,StrikeOut,ScaleX,ScaleY,Spacing,Angle,BorderStyle,Outline,Shadow,Alignment,MarginL,MarginR,MarginV,Encoding\n"
                         + "Style: Default,simhei,28,&HFFFFFFFF,&HFF000000,&HFF111111,&HFF0D0D0D,-1,0,0,0,100,100,1,0.00,1,2,0,2,30,30,10,1\n"
                         + "Style: staff,youyuan,26,&HFFEBEBEB,&HFF000000,&HD7000000,&HFF000000,-1,0,0,0,100,100,0,0.00,1,0,2,2,15,15,10,1\n",
                stylesSection.ToString());
        }
    }
}
