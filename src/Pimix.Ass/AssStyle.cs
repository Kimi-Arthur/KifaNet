using System;
using System.Collections.Generic;
using System.Drawing;

namespace Pimix.Ass {
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
                Name.GenerateAssText(),
                FontName.GenerateAssText(),
                FontSize.GenerateAssText(),
                PrimaryColour.GenerateAssText(),
                SecondaryColour.GenerateAssText(),
                OutlineColour.GenerateAssText(),
                BackColour.GenerateAssText(),
                Bold.GenerateAssText(),
                Italic.GenerateAssText(),
                Underline.GenerateAssText(),
                StrikeOut.GenerateAssText(),
                ScaleX.GenerateAssText(),
                ScaleY.GenerateAssText(),
                Spacing.GenerateAssText(),
                Angle.GenerateAssText(),
                BorderStyle.GenerateAssText(),
                Outline.GenerateAssText(),
                Shadow.GenerateAssText(),
                Alignment.GenerateAssText(),
                MarginL.GenerateAssText(),
                MarginR.GenerateAssText(),
                MarginV.GenerateAssText(),
                Encoding.GenerateAssText()
            };

        public string ValidName => Name == "Default" ? "*Default" : Name;
    }
}
