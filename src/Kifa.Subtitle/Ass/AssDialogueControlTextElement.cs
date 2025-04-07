using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

namespace Kifa.Subtitle.Ass;

public class AssDialogueControlTextElement : AssDialogueTextElement {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly Regex subElementPattern =
        new(@"\\([^\\(]*(\((?>\((?<DEPTH>)|\)(?<-DEPTH>)|[^()]+)*\)(?(DEPTH)(?!)))?)");

    static readonly Regex ValuePattern = new(@"\(|\d|-");

    public List<AssControlElement> Elements { get; set; } = new();

    public override string ToString()
        => Elements.Count == 0
            ? ""
            : "{" + string.Join("", Elements.Select(e => e.ToString())) + "}";

    public new static AssDialogueControlTextElement Parse(string content) {
        var result = new AssDialogueControlTextElement();

        content = content.Substring(1, content.Length - 2).TrimEnd(')');

        content += new string(')', content.Sum(x => (x == '(' ? 1 : 0) - (x == ')' ? 1 : 0)));

        foreach (Match elementMatch in subElementPattern.Matches(content)) {
            var elementContent = elementMatch.Groups[1].Value;
            string name, valueContent = "";
            if (elementContent.EndsWith('&')) {
                var segments = elementContent.Split('&');
                name = segments[0];
                valueContent = $"&{segments[1]}&";
            } else {
                var valueMatch = ValuePattern.Match(elementContent);
                if (!valueMatch.Success) {
                    if (elementContent.StartsWith("fn")) {
                        name = "fn";
                        valueContent = elementContent[2..];
                    } else {
                        name = elementContent;
                    }
                } else {
                    name = elementContent.Substring(0, valueMatch.Index);
                    valueContent = elementContent.Substring(valueMatch.Index);
                }
            }

            AssControlElement s;
            switch (name) {
                case BoldStyle.Key:
                    s = new BoldStyle();
                    break;
                case ItalicStyle.Key:
                    s = new ItalicStyle();
                    break;
                case UnderlineStyle.Key:
                    s = new UnderlineStyle();
                    break;
                case StrikeOutStyle.Key:
                    s = new StrikeOutStyle();
                    break;
                case BorderStyle.Key:
                    s = new BorderStyle();
                    break;
                case ShadowStyle.Key:
                    s = new ShadowStyle();
                    break;
                case BlurEdgesStyle.Key:
                    s = new BlurEdgesStyle();
                    break;
                case FontNameStyle.Key:
                    s = new FontNameStyle();
                    break;
                case FontSizeStyle.Key:
                    s = new FontSizeStyle();
                    break;
                case FontSizePercentXStyle.Key:
                    s = new FontSizePercentXStyle();
                    break;
                case FontSizePercentYStyle.Key:
                    s = new FontSizePercentYStyle();
                    break;
                case FontSpaceStyle.Key:
                    s = new FontSpaceStyle();
                    break;
                case FontRotationXStyle.Key:
                    s = new FontRotationXStyle();
                    break;
                case FontRotationYStyle.Key:
                    s = new FontRotationYStyle();
                    break;
                case FontRotationZStyle.Key:
                    s = new FontRotationZStyle();
                    break;
                case "1c":
                case PrimaryColourStyle.Key:
                    s = new PrimaryColourStyle();
                    break;
                case SecondaryColourStyle.Key:
                    s = new SecondaryColourStyle();
                    break;
                case OutlineColourStyle.Key:
                    s = new OutlineColourStyle();
                    break;
                case BackColourStyle.Key:
                    s = new BackColourStyle();
                    break;
                case AlignmentStyle.Key:
                    s = new AlignmentStyle();
                    break;
                case OldAlignmentStyle.Key:
                    s = new OldAlignmentStyle();
                    break;
                case AnimationFunction.Key:
                    s = new AnimationFunction();
                    break;
                case MoveFunction.Key:
                    s = new MoveFunction();
                    break;
                case PositionFunction.Key:
                    s = new PositionFunction();
                    break;
                case OriginFunction.Key:
                    s = new OriginFunction();
                    break;
                case FadeTimeFunction.Key:
                    s = new FadeTimeFunction();
                    break;
                case ClipFunction.Key:
                    var commaCount = (valueContent ?? "").Count(c => c == ',');
                    if (commaCount < 2) {
                        s = new DrawingClipFunction();
                    } else if (commaCount == 3) {
                        s = new TwoPointsClipFunction();
                    } else {
                        throw new ArgumentException(
                            "\\clip() function should have 1, 2, or 4 arguments.");
                    }

                    break;
                default:
                    s = new UnknownElement(name);
                    Logger.Warn("Unknown element: {0}({1})", name, valueContent);
                    break;
            }

            Logger.Trace($"{s.GetType().Name}({valueContent})");
            result.Elements.Add(s.ParseValue(valueContent));
        }

        return result;
    }
}

public abstract class AssControlElement {
    public abstract string Name { get; }

    public abstract AssControlElement ParseValue(string content);

    public override string ToString() => $"\\{Name}";

    public virtual void Scale(double scaleX, double scaleY) {
    }
}

public class UnknownElement : AssControlElement {
    public UnknownElement(string name) {
        Name = name;
    }

    public override string Name { get; }

    public string Value { get; set; }

    public override AssControlElement ParseValue(string content) {
        Value = content;
        return this;
    }

    public override string ToString() => $"\\{Name}{Value}";
}

public abstract class BoolElement : AssControlElement {
    public bool Value { get; set; } = true;

    public override AssControlElement ParseValue(string content) {
        Value = content == "1";
        return this;
    }

    public override string ToString() => $"\\{Name}{(Value ? "1" : "0")}";
}

public abstract class IntElement : AssControlElement {
    public int? Value { get; set; }

    public override AssControlElement ParseValue(string content) {
        Value = string.IsNullOrEmpty(content) ? null : (int) double.Parse(content);
        return this;
    }

    public override string ToString() => $"\\{Name}{Value}";
}

public abstract class FloatElement : AssControlElement {
    public float Value { get; set; }

    public override AssControlElement ParseValue(string content) {
        Value = float.Parse(content);
        return this;
    }

    public override string ToString() => $"\\{Name}{Value}";
}

public abstract class StringElement : AssControlElement {
    public string Value { get; set; }

    public override AssControlElement ParseValue(string content) {
        Value = content;
        return this;
    }

    public override string ToString() => $"\\{Name}{Value}";
}

public abstract class ColorElement : AssControlElement {
    public Color? Value { get; set; }

    public override AssControlElement ParseValue(string content) {
        Value = content == "" ? null : AssFormatter.ParseColor(content.TrimEnd('&'));

        return this;
    }

    public override string ToString()
        => Value == null
            ? $"\\{Name}"
            : $"\\{Name}&H{Value.Value.B:X2}{Value.Value.G:X2}{Value.Value.R:X2}&";
}

public class BoldStyle : BoolElement {
    public const string Key = "b";

    public override string Name => Key;
}

public class ItalicStyle : BoolElement {
    public const string Key = "i";

    public override string Name => Key;
}

public class UnderlineStyle : BoolElement {
    public const string Key = "u";

    public override string Name => Key;
}

public class StrikeOutStyle : BoolElement {
    public const string Key = "s";

    public override string Name => Key;
}

public class BorderStyle : IntElement {
    public const string Key = "bord";

    public override string Name => Key;
}

public class ShadowStyle : IntElement {
    public const string Key = "shad";

    public override string Name => Key;
}

public class BlurEdgesStyle : BoolElement {
    public const string Key = "be";

    public override string Name => Key;
}

public class FontNameStyle : StringElement {
    public const string Key = "fn";

    public override string Name => Key;
}

public class FontSizeStyle : IntElement {
    public const string Key = "fs";

    public override string Name => Key;

    public override void Scale(double scaleX, double scaleY) {
        Value = (Value * scaleY)?.RoundUp(10);
    }
}

public class FontSizePercentXStyle : FloatElement {
    public const string Key = "fscx";

    public override string Name => Key;
}

public class FontSizePercentYStyle : FloatElement {
    public const string Key = "fscy";

    public override string Name => Key;
}

public class FontSpaceStyle : FloatElement {
    public const string Key = "fsp";

    public override string Name => Key;
}

public class FontRotationXStyle : FloatElement {
    public const string Key = "frx";

    public override string Name => Key;
}

public class FontRotationYStyle : FloatElement {
    public const string Key = "fry";

    public override string Name => Key;
}

public class FontRotationZStyle : FloatElement {
    public const string Key = "frz";

    public override string Name => Key;
}

public class PrimaryColourStyle : ColorElement {
    public const string Key = "c";

    public override string Name => Key;
}

public class SecondaryColourStyle : ColorElement {
    public const string Key = "2c";

    public override string Name => Key;
}

public class OutlineColourStyle : ColorElement {
    public const string Key = "3c";

    public override string Name => Key;
}

public class BackColourStyle : ColorElement {
    public const string Key = "4c";

    public override string Name => Key;
}

public class AlignmentStyle : AssControlElement {
    public const string Key = "an";

    public override string Name => Key;

    public AssAlignment Value { get; set; }

    public override AssControlElement ParseValue(string content) {
        Value = (AssAlignment) int.Parse(content);
        return this;
    }

    public override string ToString() => $"\\{Name}{Value:d}";
}

public class OldAlignmentStyle : AlignmentStyle {
    public new const string Key = "a";

    public override AssControlElement ParseValue(string content) {
        var index = int.Parse(content);
        if (index < 4) {
            Value = (AssAlignment) index;
        } else if (index < 7) {
            Value = (AssAlignment) index + 3;
        } else {
            Value = (AssAlignment) index - 3;
        }

        return this;
    }
}

public abstract class OnePointFunction : AssControlElement {
    public PointF Position { get; set; }

    public override AssControlElement ParseValue(string content) {
        var segments = content.Substring(1, content.Length - 2).Split(',').Select(s => s.Trim())
            .ToList();
        Position = new PointF(float.Parse(segments[0]), float.Parse(segments[1]));
        return this;
    }

    public override void Scale(double scaleX, double scaleY) {
        Position = new PointF((Position.X * scaleX).RoundUp(10), (Position.Y * scaleY).RoundUp(10));
    }

    public override string ToString() => $"\\{Name}({Position.X},{Position.Y})";
}

public class AnimationFunction : AssControlElement {
    public const string Key = "t";

    public override string Name => Key;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public double Acceleration { get; set; } = -1;

    public List<AssControlElement> Inner { get; set; }

    public override AssControlElement ParseValue(string content) {
        var segments = content.Substring(1, content.Length - 2).Split('\\', 2).Select(s => s.Trim())
            .ToList();

        var optionSegments = segments[0].Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (optionSegments.Length == 3 || optionSegments.Length == 2) {
            StartTime = TimeSpan.FromMilliseconds(int.Parse(optionSegments[0]));
            EndTime = TimeSpan.FromMilliseconds(int.Parse(optionSegments[1]));
        }

        if (optionSegments.Length == 3 || optionSegments.Length == 1) {
            Acceleration = double.Parse(optionSegments.Last());
        }

        Inner = AssDialogueControlTextElement.Parse($"{{\\{segments.Last()}}}").Elements.ToList();

        return this;
    }

    public override void Scale(double scaleX, double scaleY) {
        foreach (var element in Inner) {
            element.Scale(scaleX, scaleY);
        }
    }

    public override string ToString() {
        var result = $"\\{Name}(";
        if (StartTime != TimeSpan.Zero || EndTime != TimeSpan.Zero) {
            result += $"{StartTime.TotalMilliseconds},{EndTime.TotalMilliseconds},";
        }

        if (Acceleration >= 0) {
            result += $"{Acceleration},";
        }

        return $"{result}{string.Join("", Inner.Select(i => i.ToString()))})";
    }
}

public class MoveFunction : AssControlElement {
    public const string Key = "move";

    public override string Name => Key;

    public PointF StartPosition { get; set; }
    public PointF EndPosition { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public override AssControlElement ParseValue(string content) {
        var segments = content.Substring(1, content.Length - 2).Split(',').Select(s => s.Trim())
            .ToList();
        StartPosition = new PointF(float.Parse(segments[0]), float.Parse(segments[1]));
        EndPosition = new PointF(float.Parse(segments[2]), float.Parse(segments[3]));

        if (segments.Count == 6) {
            StartTime = TimeSpan.FromMilliseconds(int.Parse(segments[4]));
            EndTime = TimeSpan.FromMilliseconds(int.Parse(segments[5]));
        }

        return this;
    }

    public override void Scale(double scaleX, double scaleY) {
        StartPosition = new PointF((StartPosition.X * scaleX).RoundUp(10),
            (StartPosition.Y * scaleY).RoundUp(10));

        EndPosition = new PointF((EndPosition.X * scaleX).RoundUp(10),
            (EndPosition.Y * scaleY).RoundUp(10));
    }

    public override string ToString() {
        var result =
            $"\\{Name}({StartPosition.X},{StartPosition.Y},{EndPosition.X},{EndPosition.Y}";
        if (StartTime != TimeSpan.Zero || EndTime != TimeSpan.Zero) {
            result += $",{StartTime.TotalMilliseconds},{EndTime.TotalMilliseconds}";
        }

        return result + ")";
    }
}

public class PositionFunction : OnePointFunction {
    public const string Key = "pos";

    public override string Name => Key;
}

public class OriginFunction : OnePointFunction {
    public const string Key = "org";

    public override string Name => Key;
}

public class FadeTimeFunction : AssControlElement {
    public const string Key = "fad";

    public TimeSpan FadeInTime { get; set; }

    public TimeSpan FadeOutTime { get; set; }

    public override string Name => Key;

    public override AssControlElement ParseValue(string content) {
        var segments = content.Substring(1, content.Length - 2).Split(',').Select(s => s.Trim())
            .ToList();
        FadeInTime = TimeSpan.FromMilliseconds(int.Parse(segments[0]));
        FadeOutTime = TimeSpan.FromMilliseconds(int.Parse(segments[1]));
        return this;
    }

    public override string ToString()
        => $"\\{Name}({FadeInTime.TotalMilliseconds},{FadeOutTime.TotalMilliseconds})";
}

public abstract class ClipFunction : AssControlElement {
    public const string Key = "clip";

    public override string Name => Key;
}

public class TwoPointsClipFunction : ClipFunction {
    public PointF Position1 { get; set; }
    public PointF Position2 { get; set; }

    public override AssControlElement ParseValue(string content) {
        var segments = content.Substring(1, content.Length - 2).Split(',').Select(s => s.Trim())
            .ToList();
        Position1 = new PointF(float.Parse(segments[0]), float.Parse(segments[1]));
        Position2 = new PointF(float.Parse(segments[2]), float.Parse(segments[3]));
        return this;
    }

    public override void Scale(double scaleX, double scaleY) {
        Position1 = new PointF((Position1.X * scaleX).RoundUp(10),
            (Position1.Y * scaleY).RoundUp(10));

        Position2 = new PointF((Position2.X * scaleX).RoundUp(10),
            (Position2.Y * scaleY).RoundUp(10));
    }

    public override string ToString()
        => $"\\{Name}({Position1.X},{Position1.Y},{Position2.X},{Position2.Y})";
}

public class DrawingClipFunction : ClipFunction {
    const int DefaultScaleDownLevel = 1;
    public int ScaleDownLevel { get; set; } = DefaultScaleDownLevel;
    public List<AssDrawingCommand> DrawingCommands { get; set; } = new();

    public override AssControlElement ParseValue(string content) {
        var segments = content.Substring(1, content.Length - 2).Split(',').Select(s => s.Trim())
            .ToList();
        if (segments.Count == 2) {
            ScaleDownLevel = int.Parse(segments[0]);
        }

        AssDrawingCommand current = null;
        float? currentX = null;
        foreach (var s in segments.Last().Split(' ')) {
            if (char.IsLetter(s, 0)) {
                if (current != null) {
                    DrawingCommands.Add(current);
                }

                current = new AssDrawingCommand {
                    Name = s
                };

                continue;
            }

            if (current == null) {
                throw new ArgumentException("Drawing commands should start with a command name.");
            }

            if (currentX != null) {
                current.Points.Add(new PointF(currentX.Value, float.Parse(s)));
                currentX = null;
            } else {
                currentX = float.Parse(s);
            }
        }

        if (current != null) {
            DrawingCommands.Add(current);
        }

        return this;
    }

    public override void Scale(double scaleX, double scaleY) {
        foreach (var command in DrawingCommands) {
            command.Scale(scaleX, scaleY);
        }
    }

    public override string ToString() {
        var commands = string.Join(' ', DrawingCommands.Select(c => c.ToString()));
        return ScaleDownLevel == DefaultScaleDownLevel
            ? $"\\{Name}({commands})"
            : $"\\{Name}({ScaleDownLevel}, {commands})";
    }
}
