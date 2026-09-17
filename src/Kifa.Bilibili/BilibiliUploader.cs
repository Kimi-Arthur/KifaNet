using System;
using System.Collections.Generic;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliUploader : DataModel, WithModelId<BilibiliUploader> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "bilibili/uploaders";

    public static KifaServiceClient<BilibiliUploader> Client { get; set; } =
        new KifaServiceRestClient<BilibiliUploader>();

    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(30);

    public string? Name { get; set; }

    // Legacy fields preserved for Stage 1 migration.
    public List<string> Aids { get; set; } = [];
    public List<string> RemovedAids { get; set; } = [];

    public string GetUploaderFolder()
        => $"{Name.Checked().NormalizeFileName().Choppable()}.{Id}.bilibili".NormalizeFileName(
            reservedBytes: 0, maxByteCount: PathExtensions.MaxPathSegmentByteCount);

    public override void Fill(bool deep = false) {
        var info = HttpClients.GetBilibiliClient().Call(new UploaderInfoWebRpc(Id.Checked()));
        if (info == null) {
            throw new DataNotFoundException(
                $"Failed to retrieve data for uploader ({Id}) from bilibili.");
        }

        Name = info.Space.Info.Name;
    }
}
