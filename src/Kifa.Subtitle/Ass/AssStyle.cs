using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kifa.Subtitle.Ass;

public class AssStyle : AssLine {
    public enum BorderStyleType {
        OutlineWithDropShadow = 1,
        OpaqueBox = 3
    }

    public const string DefaultFontName = "Arial";
    public const int DefaultSemiAlpha = 224;

    int shadow = 1;

    public static AssStyle DefaultStyle { get; set; } = new() {
        Name = "Default",
        Alignment = AssAlignment.BottomCenter
    };

    public static AssStyle SubtitleStyle { get; set; } = new() {
        Name = "Subtitle",
        Alignment = AssAlignment.BottomCenter,
        FontSize = 80,
        MarginV = 20
    };

    public static AssStyle ToptitleStyle { get; set; } = new() {
        Name = "Toptitle",
        Alignment = AssAlignment.TopCenter,
        FontSize = 80,
        MarginV = 20
    };

    public static AssStyle NormalCommentStyle { get; set; } = new() {
        Name = "NormalComment",
        Alignment = AssAlignment.TopCenter,
        PrimaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
        SecondaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
        OutlineColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
        BackColour = Color.FromArgb(DefaultSemiAlpha, Color.Black)
    };

    public static AssStyle RtlCommentStyle { get; set; } = new() {
        Name = "RtlComment",
        Alignment = AssAlignment.TopCenter,
        PrimaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
        SecondaryColour = Color.FromArgb(DefaultSemiAlpha, Color.White),
        OutlineColour = Color.FromArgb(DefaultSemiAlpha, Color.Black),
        BackColour = Color.FromArgb(DefaultSemiAlpha, Color.Black)
    };

    public static AssStyle TopCommentStyle { get; set; } = new() {
        Name = "TopComment",
        Alignment = AssAlignment.TopCenter,
        PrimaryColour = Color.White,
        SecondaryColour = Color.White,
        OutlineColour = Color.Black,
        BackColour = Color.Black
    };

    public static AssStyle BottomCommentStyle { get; set; } = new() {
        Name = "BottomComment",
        Alignment = AssAlignment.BottomCenter,
        PrimaryColour = Color.White,
        SecondaryColour = Color.White,
        OutlineColour = Color.Black,
        BackColour = Color.Black
    };

    public static List<AssStyle> Styles
        => new() {
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

    public double Spacing { get; set; }

    public double Angle { get; set; }

    public BorderStyleType BorderStyle { get; set; } = BorderStyleType.OutlineWithDropShadow;

    public int Outline { get; set; } = 1;

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
            PrimaryColour.ToAss(),
            SecondaryColour.ToAss(),
            OutlineColour.ToAss(),
            BackColour.ToAss(),
            Bold.ToAss(),
            Italic.ToAss(),
            Underline.ToAss(),
            StrikeOut.ToAss(),
            ScaleX.ToString(),
            ScaleY.ToString(),
            $"{Spacing:f2}",
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

    public AssStyle Scale(double scale) {
        FontSize = (FontSize * scale).RoundUp(10);
        MarginL = (MarginL * scale).RoundUp(10);
        MarginR = (MarginR * scale).RoundUp(10);
        MarginV = (MarginV * scale).RoundUp(10);

        return this;
    }

    public static AssStyle Parse(IEnumerable<string> content, IEnumerable<string> headers) {
        var style = new AssStyle();
        foreach (var p in content.Zip(headers, Tuple.Create)) {
            switch (p.Item2) {
                case "Name":
                    style.Name = p.Item1;
                    break;
                case "Fontname":
                    style.FontName = p.Item1;
                    break;
                case "Fontsize":
                    style.FontSize = int.Parse(p.Item1);
                    break;
                case "PrimaryColour":
                    style.PrimaryColour = AssFormatter.ParseColor(p.Item1);
                    break;
                case "SecondaryColour":
                    style.SecondaryColour = AssFormatter.ParseColor(p.Item1);
                    break;
                case "OutlineColour":
                    style.OutlineColour = AssFormatter.ParseColor(p.Item1);
                    break;
                case "BackColour":
                    style.BackColour = AssFormatter.ParseColor(p.Item1);
                    break;
                case "Bold":
                    style.Bold = AssFormatter.ParseBool(p.Item1);
                    break;
                case "Italic":
                    style.Italic = AssFormatter.ParseBool(p.Item1);
                    break;
                case "Underline":
                    style.Underline = AssFormatter.ParseBool(p.Item1);
                    break;
                case "Strikeout":
                    style.StrikeOut = AssFormatter.ParseBool(p.Item1);
                    break;
                case "ScaleX":
                    style.ScaleX = int.Parse(p.Item1);
                    break;
                case "ScaleY":
                    style.ScaleY = int.Parse(p.Item1);
                    break;
                case "Spacing":
                    style.Spacing = double.Parse(p.Item1);
                    break;
                case "Angle":
                    style.Angle = double.Parse(p.Item1);
                    break;
                case "BorderStyle":
                    style.BorderStyle = (BorderStyleType) int.Parse(p.Item1);
                    break;
                case "Outline":
                    style.Outline = double.Parse(p.Item1).RoundUp(1);
                    break;
                case "Shadow":
                    style.Shadow = double.Parse(p.Item1).RoundUp(1);
                    break;
                case "Alignment":
                    style.Alignment = (AssAlignment) int.Parse(p.Item1);
                    break;
                case "MarginL":
                    style.MarginL = int.Parse(p.Item1);
                    break;
                case "MarginR":
                    style.MarginR = int.Parse(p.Item1);
                    break;
                case "MarginV":
                    style.MarginV = int.Parse(p.Item1);
                    break;
                case "Encoding":
                    style.Encoding = int.Parse(p.Item1);
                    break;
            }
        }

        return style;
    }
}
