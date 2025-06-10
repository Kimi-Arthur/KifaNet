using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliBangumi : DataModel, WithModelId<BilibiliBangumi> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "bilibili/bangumis";

    public static KifaServiceClient<BilibiliBangumi> Client { get; set; } =
        new KifaServiceRestClient<BilibiliBangumi>();

    public string SeasonId { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public List<string> Aids { get; set; }
    public List<string> ExtraAids { get; set; } = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var mediaData = HttpClients.GetBilibiliClient().Call(new MediaRpc(Id))?.Result;
        SeasonId = $"ss{mediaData.Media.SeasonId}";
        Title = mediaData.Media.Title.Trim();
        Type = mediaData.Media.TypeName;
        var seasonData = HttpClients.GetBilibiliClient().Call(new MediaSeasonRpc(SeasonId))?.Result;
        if (seasonData == null) {
            throw new UnableToFillException(
                $"Failed to get data for season ({SeasonId}) from Bilibili.");
        }

        Aids = seasonData.MainSection.Episodes.Select(e => $"av{e.Aid}").ToList();
        ExtraAids = seasonData.Section.SelectMany(s => s.Episodes.Select(e => $"av{e.Aid}"))
            .ToList();

        return null;
    }
}
