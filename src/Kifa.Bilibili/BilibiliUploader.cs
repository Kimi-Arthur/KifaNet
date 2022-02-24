using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;

namespace Kifa.Bilibili; 

public class BilibiliUploader : DataModel<BilibiliUploader> {
    public const string ModelId = "bilibili/uploaders";

    static KifaServiceClient<BilibiliUploader> client;

    public static KifaServiceClient<BilibiliUploader> Client =>
        client ??= new KifaServiceRestClient<BilibiliUploader>();

    public string Name { get; set; }
    public List<string> Aids { get; set; } = new();
    public List<string> RemovedAids { get; set; } = new();

    public override bool? Fill() {
        var info = new UploaderInfoRpc().Call(Id).Data;
        Name = info.Name;
        var list = new UploaderVideoRpc().Call(Id).Data.List.Vlist.Select(v => $"av{v.Aid}").ToHashSet();

        var removed = RemovedAids.ToHashSet();
        removed.UnionWith(Aids);
        removed.ExceptWith(list);

        RemovedAids = removed.OrderBy(v => v).ToList();
        Aids = list.OrderBy(v => v).ToList();

        return true;
    }
}