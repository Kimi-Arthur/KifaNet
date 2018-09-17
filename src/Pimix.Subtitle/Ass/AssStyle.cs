using System;
using System.Collections.Generic;
using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public class AssStyle : AssLine {
        public enum BorderStyleType {
            OutlineWithDropShadow = 1,
            OpaqueBox = 3
        }

        public const string DefaultFontName = "Simhei";

        public static readonly AssStyle DefaultStyle
            = new AssStyle {
                Name = "Default"
            };

        public static readonly AssStyle SubtitleStyle
            = new AssStyle {
                Name = "Subtitle"
            };

        public static readonly AssStyle ToptitleStyle
            = new AssStyle {
                Name = "Toptitle"
            };

        public static readonly AssStyle TopCommentStyle
            = new AssStyle {
                Name = "TopComment"
            };

        public static readonly AssStyle BottomCommentStyle
            = new AssStyle {
                Name = "BottomComment"
            };

        public static readonly AssStyle RtlCommentStyle
            = new AssStyle {
                Name = "RtlComment"
            };

        public static readonly List<AssStyle> Styles =
            new List<AssStyle> {
                DefaultStyle,
                SubtitleStyle,
                ToptitleStyle,
                TopCommentStyle,
                BottomCommentStyle,
                RtlCommentStyle
            };

        public string Name { get; set; }

        public string FontName { get; set; } = DefaultFontName;

        public int FontSize { get; set; }

        public Color PrimaryColour { get; set; }

        public Color SecondaryColour { get; set; }

        public Color OutlineColour { get; set; }

        public Color BackColour { get; set; }

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public bool Underline { get; set; }

        public bool StrikeOut { get; set; }

        public int ScaleX { get; set; }

        public int ScaleY { get; set; }

        public int Spacing { get; set; }

        public double Angle { get; set; }

        public BorderStyleType BorderStyle { get; set; }

        int outline;

        public int Outline {
            get => outline;
            set {
                if (value < 0 || value > 4) {
                    throw new ArgumentOutOfRangeException(nameof(Outline));
                }

                outline = value;
            }
        }

        int shadow;

        public int Shadow {
            get => shadow;
            set {
                if (value < 0 || value > 4) {
                    throw new ArgumentOutOfRangeException(nameof(Shadow));
                }

                shadow = value;
            }
        }

        public AssAlignment Alignment { get; set; }

        public int MarginL { get; set; }

        public int MarginR { get; set; }

        public int MarginV { get; set; }

        public int Encoding { get; set; }

        public override string Key => "Style";

        public override IEnumerable<string> Values
            => new List<string> {
                Name,
                FontName,
                FontSize.ToString(),
                $"&H{PrimaryColour.A:X2}{PrimaryColour.B:X2}{PrimaryColour.G:X2}{PrimaryColour.R:X2}",
                $"&H{SecondaryColour.A:X2}{SecondaryColour.B:X2}{SecondaryColour.G:X2}{SecondaryColour.R:X2}",
                $"&H{OutlineColour.A:X2}{OutlineColour.B:X2}{OutlineColour.G:X2}{OutlineColour.R:X2}",
                $"&H{BackColour.A:X2}{BackColour.B:X2}{BackColour.G:X2}{BackColour.R:X2}",
                Bold ? "-1" : "0",
                Italic ? "-1" : "0",
                Underline ? "-1" : "0",
                StrikeOut ? "-1" : "0",
                ScaleX.ToString(),
                ScaleY.ToString(),
                Spacing.ToString(),
                $"{Angle:f2}",
                $"{BorderStyle:d}",
                Outline.ToString(),
                Shadow.ToString(),
                $"{Alignment:d}",
                MarginL.ToString(),
                MarginR.ToString(),
                MarginV.ToString(),
                Encoding.ToString()
            };

        public string ValidName => Name == "Default" ? "*Default" : Name;
    }
}
