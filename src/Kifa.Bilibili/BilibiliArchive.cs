using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;

namespace Kifa.Bilibili;

public class BilibiliArchive : DataModel, WithModelId<BilibiliArchive> {
    public static string ModelId => "bilibili/archives";

    public static KifaServiceClient<BilibiliArchive> Client { get; set; } =
        new KifaServiceRestClient<BilibiliArchive>();

    public string? AuthorId { get; set; }
    public string? SeasonId { get; set; }

    public string? Author { get; set; }
    public string? Title { get; set; }

    public List<string> Videos { get; set; } = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var ids = Id.Split("/");
        AuthorId = ids[0];
        SeasonId = ids[1];

        var info = HttpClients.BilibiliHttpClient.Call(new UploaderInfoRpc(AuthorId)).Data;
        if (info == null) {
            throw new DataNotFoundException(
                $"Failed to retrieve data for uploader ({Id}) from bilibili,");
        }

        Author = info.Name;
        var data = HttpClients.BilibiliHttpClient
            .Call(new ArchiveRpc(uploaderId: AuthorId, seasonId: SeasonId)).Data;
        if (data == null) {
            throw new DataNotFoundException($"Failed to find archive ({Id}).");
        }

        Title = data.Meta.Checked().Name;

        Videos = data.Aids.Select(m => $"av{m}").ToList();
        var page = 1;
        while (Videos.Count < data.Page.Checked().Total) {
            data = HttpClients.BilibiliHttpClient
                .Call(new ArchiveRpc(uploaderId: AuthorId, seasonId: SeasonId, page: ++page)).Data;
            if (data == null) {
                throw new DataNotFoundException($"Failed to find playlist ({Id}).");
            }

            Videos.AddRange(data.Aids.Select(m => $"av{m}"));
        }

        return null;
    }

    public string GetBaseFolder() => $"{Author}-{Title}.{AuthorId}-{SeasonId}";
}
