using System.Drawing;
using System.Globalization;
using Kifa.Tencent.Rpcs;
using Newtonsoft.Json;

namespace Kifa.Tencent;

public class TencentDanmu {
    public required string Text { get; set; }

    public Color? TextColor { get; set; }

    public required TimeSpan VideoTime { get; set; }

    internal static TencentDanmu Parse(SegmentDanmuRpc.Barrage barrage) {
        var color = barrage.ContentStyle != null
            ? JsonConvert.DeserializeObject<ContentStyle>(barrage.ContentStyle,
                KifaJsonSerializerSettings.Default)?.Color
            : null;

        return new TencentDanmu {
            Text = barrage.Content,
            VideoTime = TimeSpan.FromMilliseconds(barrage.TimeOffset),
            TextColor = color == null
                ? null
                : Color.FromArgb(int.Parse(color[..2], NumberStyles.HexNumber),
                    int.Parse(color[2..4], NumberStyles.HexNumber),
                    int.Parse(color[4..], NumberStyles.HexNumber))
        };
    }
}

class ContentStyle {
    // {"color":"ffffff","gradient_colors":["CD87FF","CD87FF"],"position":1}
    public string Color { get; set; }
    public List<string> GradientColors { get; set; }
    public int Position { get; set; }
}
