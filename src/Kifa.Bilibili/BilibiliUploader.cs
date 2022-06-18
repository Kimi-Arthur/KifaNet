using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliUploader : DataModel<BilibiliUploader> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string ModelId = "bilibili/uploaders";

    static KifaServiceClient<BilibiliUploader>? client;

    public static KifaServiceClient<BilibiliUploader> Client
        => client ??= new KifaServiceRestClient<BilibiliUploader>();

    public string Name { get; set; }
    public List<string> Aids { get; set; } = new();
    public List<string> RemovedAids { get; set; } = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var info = new UploaderInfoRpc().Invoke(Id)?.Data;
        if (info == null) {
            throw new DataNotFoundException(
                $"Failed to retrieve data for uploader ({Id}) from bilibili,");
        }

        Name = info.Name;
        var list = new UploaderVideoRpc().Invoke(Id).Data.List.Vlist.Select(v => $"av{v.Aid}")
            .ToHashSet();

        var removed = RemovedAids.ToHashSet();
        removed.UnionWith(Aids);
        removed.ExceptWith(list);

        RemovedAids = removed.OrderBy(v => long.Parse(v[2..])).ToList();
        Aids = list.OrderBy(v => long.Parse(v[2..])).ToList();

        return Date.Zero;
    }
}
