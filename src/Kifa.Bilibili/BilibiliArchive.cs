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


    public override void Fill(bool deep = false) {
        var ids = Id.Split("/");
        AuthorId = ids[0];
        SeasonId = ids[1];

        var info = HttpClients.GetBilibiliClient().Call(new UploaderInfoWebRpc(AuthorId));
        if (info == null) {
            throw new FailedToFillException(
                $"Failed to retrieve data for uploader ({AuthorId}) from bilibili.");
        }

        Author = info.Space.Info.Name;
        var response = HttpClients.GetBilibiliClient()
            .Call(new ArchiveRpc(uploaderId: AuthorId, seasonId: SeasonId));
        if (response?.Code is -404 or -400 or 12002) {
            throw new DataNotFoundException($"Failed to find archive ({Id}): {response?.Message}.");
        }

        if (response?.Data == null) {
            throw new FailedToFillException(
                $"Failed to retrieve archive ({Id}): {response?.Message} ({response?.Code}).");
        }

        var data = response.Data;
        Title = data.Meta.Checked().Name;

        Videos = data.Aids.Select(m => $"av{m}").ToList();
        var page = 1;
        while (Videos.Count < data.Page.Checked().Total) {
            response = HttpClients.GetBilibiliClient()
                .Call(new ArchiveRpc(uploaderId: AuthorId, seasonId: SeasonId, page: ++page));
            if (response?.Data == null) {
                throw new FailedToFillException(
                    $"Failed to retrieve archive ({Id}) page {page}: {response?.Message} ({response?.Code}).");
            }

            data = response.Data;
            Videos.AddRange(data.Aids.Select(m => $"av{m}"));
        }
    }

    const string ArchivePrefix = "合集·";

    public string GetArchiveFolder()
        => $"{Title.Checked().ChopPrefix(ArchivePrefix).NormalizeFileName().Choppable()}.{SeasonId}";
}
