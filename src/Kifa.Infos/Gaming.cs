using System.Text.RegularExpressions;

namespace Kifa.Infos;

public class Gaming : Formattable {
    static readonly string PREFIX = "/Gaming";

    public string Id { get; set; }

    // Only one season is currently supported.
    // Reference example: /Gaming/黑桐谷歌/漫威蜘蛛侠2/漫威蜘蛛侠2 EP01
    public string? Format(Season season, Episode episode)
        => $"{PREFIX}/{Id}/{Id.Split("/")[^1]} EP{episode.Id.ToString().PadLeft(2, '0')}";

    // Reference example: /Gaming/黑桐谷歌/漫威蜘蛛侠2/漫威蜘蛛侠2 EP01 表面张力.mp4
    public (Season Season, Episode Episode)? Parse(string formatted) {
        var pattern = $@"{PREFIX}/{Id}/{Id.Split("/")[^1]} EP(?<episode_id>\d+)";

        var match = Regex.Match(formatted, pattern);
        return match.Success && match.Groups["episode_id"].Success
            ? (new Season {
                Id = 1
            }, new Episode {
                Id = int.Parse(match.Groups["episode_id"].Value)
            })
            : null;
    }
}
