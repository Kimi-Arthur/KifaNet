using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliBangumi : DataModel<BilibiliBangumi> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string ModelId = "bilibili/bangumis";

    static KifaServiceClient<BilibiliBangumi> client;

    public static KifaServiceClient<BilibiliBangumi> Client
        => client ??= new KifaServiceRestClient<BilibiliBangumi>();

    public string SeasonId { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public List<string> Aids { get; set; }
    public List<string> ExtraAids { get; set; }

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var mediaData = new MediaRpc().Invoke(Id).Result;
        SeasonId = $"ss{mediaData.Media.SeasonId}";
        Title = mediaData.Media.Title;
        Type = mediaData.Media.TypeName;
        var seasonData = new MediaSeasonRpc().Invoke(SeasonId)?.Result;
        if (seasonData == null) {
            Logger.Error($"Failed to get data for season ({SeasonId}) from Bilibili.");
            return Date.Zero;
        }

        Aids = seasonData.MainSection.Episodes.Select(e => $"av{e.Aid}").ToList();
        ExtraAids = seasonData.Section.SelectMany(s => s.Episodes.Select(e => $"av{e.Aid}"))
            .ToList();

        return Date.Zero;
    }
}
