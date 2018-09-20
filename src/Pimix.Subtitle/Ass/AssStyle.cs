using System;
using System.Collections.Generic;
using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public class AssStyle : AssLine {
        public enum BorderStyleType {
            OutlineWithDropShadow = 1,
            OpaqueBox = 3
        }

        public const string DefaultFontName = "Arial";
        public const int DefaultSemiAlpha = 160;

        public static AssStyle DefaultStyle { get; set; }
            = new AssStyle {
                Name = "Default",
                Alignment = AssAlignment.BottomCenter
            };

        public static AssStyle SubtitleStyle { get; set; }
            = new AssStyle {
                Name = "Subtitle",
                Alignment = AssAlignment.BottomCenter,
                FontSize = 80,
                MarginV = 20
            };

        public static AssStyle ToptitleStyle { get; set; }
            = new AssStyle {
                Name = "Toptitle",
                Alignment = AssAlignment.TopCenter,
                FontSize = 80,
                MarginV = 20
            };

        public static AssStyle NormalCommentStyle { get; set; }
            = new AssStyle {
                Name = "NormalComment",
                Alignment = AssAlignment.TopCenter,
                PrimaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                SecondaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                OutlineColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
                BackColour = Color.FromArgb(DefaultSemiAlpha, Color.Black)
            };

        public static AssStyle RtlCommentStyle { get; set; }
            = new AssStyle {
                Name = "RtlComment",
                Alignment = AssAlignment.TopCenter,
                PrimaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                SecondaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                OutlineColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
                BackColour = Color.FromArgb(DefaultSemiAlpha, Color.Black)
            };

        public static AssStyle TopCommentStyle { get; set; }
            = new AssStyle {
                Name = "TopComment",
                Alignment = AssAlignment.TopCenter,
                PrimaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                SecondaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                OutlineColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
                BackColour = Color.FromArgb(DefaultSemiAlpha, Color.Black)
            };

        public static AssStyle BottomCommentStyle { get; set; }
            = new AssStyle {
                Name = "BottomComment",
                Alignment = AssAlignment.BottomCenter,
                PrimaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                SecondaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
                OutlineColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
                BackColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
                MarginV = 200 // Avoid subtitle area.
            };

        public static readonly List<AssStyle> Styles =
            new List<AssStyle> {
                DefaultStyle,
                SubtitleStyle,
                ToptitleStyle,
                NormalCommentStyle,
                TopCommentStyle,
                BottomCommentStyle,
                RtlCommentStyle
            };

        public string Name { get; set; }

        public string FontName { get; set; } = DefaultFontName;

        public int FontSize { get; set; } = 50;

        public Color PrimaryColour { get; set; } = Color.White;

        public Color SecondaryColour { get; set; } = Color.White;

        public Color OutlineColour { get; set; } = Color.Black;

        public Color BackColour { get; set; } = Color.Black;

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public bool Underline { get; set; }

        public bool StrikeOut { get; set; }

        public int ScaleX { get; set; } = 100;

        public int ScaleY { get; set; } = 100;

        public int Spacing { get; set; }

        public double Angle { get; set; }

        public BorderStyleType BorderStyle { get; set; } = BorderStyleType.OutlineWithDropShadow;

        int outline = 1;

        public int Outline {
            get => outline;
            set {
                if (value < 0 || value > 4) {
                    throw new ArgumentOutOfRangeException(nameof(Outline));
                }

                outline = value;
            }
        }

        int shadow = 1;

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
                AssFormatter.ToString(PrimaryColour),
                AssFormatter.ToString(SecondaryColour),
                AssFormatter.ToString(OutlineColour),
                AssFormatter.ToString(BackColour),
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
