using System.Drawing;
using System.Globalization;
using Kifa.Subtitle.Ass;
using Kifa.Tencent.Rpcs;
using Newtonsoft.Json;

namespace Kifa.Tencent;

public class TencentDanmu {
    public string Id { get; set; }
    public int IsOp { get; set; }
    public string HeadUrl { get; set; }
    public long TimeOffset { get; set; }
    public string UpCount { get; set; }
    public string BubbleHead { get; set; }
    public string BubbleLevel { get; set; }
    public string BubbleId { get; set; }
    public int RickType { get; set; }

    // Example: {"color":"ffffff","gradient_colors":["CD87FF","CD87FF"],"position":1}
    public string? ContentStyle { get; set; }
    public int UserVipDegree { get; set; }
    public string CreateTime { get; set; }

    #region public late string Content { get; set; }

    string? content;

    public string Content {
        get => Late.Get(content);
        set => Late.Set(ref content, value);
    }

    #endregion

    public long HotType { get; set; }
    public string Vuid { get; set; }
    public string Nick { get; set; }
    public string DataKey { get; set; }
    public double ContentScore { get; set; }
    public long ShowWeight { get; set; }
    public long TrackType { get; set; }
    public long ShowLikeType { get; set; }
    public long ReportLikeScore { get; set; }

    static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(8);

    public AssDialogue GenerateAssDialogue() {
        var colorString = !string.IsNullOrEmpty(ContentStyle)
            ? JsonConvert.DeserializeObject<ContentStyle>(ContentStyle,
                KifaJsonSerializerSettings.Default)?.GradientColors?[0]
            : null;
        if (colorString == "ffffff") {
            colorString = null;
        }

        var textElements = new List<AssDialogueTextElement>();
        if (colorString != null) {
            textElements.Add(new AssDialogueControlTextElement {
                Elements = new List<AssControlElement> {
                    new PrimaryColourStyle {
                        Value = Color.FromArgb(AssStyle.DefaultSemiAlpha,
                            int.Parse(colorString[..2], NumberStyles.HexNumber),
                            int.Parse(colorString[2..4], NumberStyles.HexNumber),
                            int.Parse(colorString[4..], NumberStyles.HexNumber))
                    }
                }
            });
        }

        textElements.Add(new AssDialogueRawTextElement {
            Content = Content.Trim()
        });

        return new AssDialogue {
            Start = TimeSpan.FromMilliseconds(TimeOffset),
            End = TimeSpan.FromMilliseconds(TimeOffset) + DefaultDuration,
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
