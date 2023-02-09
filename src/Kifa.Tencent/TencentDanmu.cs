using System.Drawing;
using System.Globalization;
using Kifa.Subtitle.Ass;
using Kifa.Tencent.Rpcs;
using Newtonsoft.Json;

namespace Kifa.Tencent;

public class TencentDanmu {
    static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(8);

    public required string Text { get; set; }

    public Color? TextColor { get; set; }

    public required TimeSpan VideoTime { get; set; }

    internal static TencentDanmu Parse(SegmentDanmuRpc.Barrage barrage) {
        var color = !string.IsNullOrEmpty(barrage.ContentStyle)
            ? JsonConvert.DeserializeObject<ContentStyle>(barrage.ContentStyle,
                KifaJsonSerializerSettings.Default)?.GradientColors?[0]
            : null;
        if (color == "ffffff") {
            color = null;
        }

        return new TencentDanmu {
            Text = barrage.Content,
            VideoTime = TimeSpan.FromMilliseconds(barrage.TimeOffset),
            TextColor = color == null
                ? null
                : Color.FromArgb(AssStyle.DefaultSemiAlpha,
                    int.Parse(color[..2], NumberStyles.HexNumber),
                    int.Parse(color[2..4], NumberStyles.HexNumber),
                    int.Parse(color[4..], NumberStyles.HexNumber))
        };
    }

    public AssDialogue GenerateAssDialogue() {
        var textElements = new List<AssDialogueTextElement>();
        if (TextColor.HasValue) {
            textElements.Add(new AssDialogueControlTextElement {
                Elements = new List<AssControlElement> {
                    new PrimaryColourStyle {
                        Value = TextColor.Value
                    }
                }
            });
        }

        textElements.Add(new AssDialogueRawTextElement {
            Content = Text.Trim()
        });

        return new AssDialogue {
            Start = VideoTime,
            End = VideoTime + DefaultDuration,
            Layer = 0,
            Text = new AssDialogueText {
                TextElements = textElements
            },
            Style = AssStyle.NormalCommentStyle
        };
    }
}

class ContentStyle {
    // {"color":"ffffff","gradient_colors":["CD87FF","CD87FF"],"position":1}
    public string Color { get; set; }
    public List<string>? GradientColors { get; set; }
    public int Position { get; set; }
}
