using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;

namespace Kifa.Bilibili;

public class BilibiliPlaylist : DataModel, WithModelId<BilibiliPlaylist> {
    public static string ModelId => "bilibili/playlists";

    public static KifaServiceClient<BilibiliPlaylist> Client { get; set; } =
        new KifaServiceRestClient<BilibiliPlaylist>();

    public string Title { get; set; }
    public string Uploader { get; set; }

    public List<string> Videos { get; set; }


    public override void Fill(bool deep = false) {
        var response = HttpClients.GetBilibiliClient().Call(new PlaylistRpc(Id));
        if (response?.Code is -404 or 11010 or 53000) {
            throw new DataNotFoundException($"Failed to find playlist ({Id}): {response?.Message}.");
        }

        if (response?.Data == null) {
            throw new FailedToFillException(
                $"Failed to retrieve playlist ({Id}): {response?.Message} ({response?.Code}).");
        }

        var data = response.Data;
        Title = data.Info.Title;
        Uploader = data.Info.Upper.Name;
        Videos = data.Medias.Select(m => $"av{m.Id}").ToList();
        var page = 1;
        while (data.HasMore) {
            response = HttpClients.GetBilibiliClient().Call(new PlaylistRpc(Id, ++page));
            if (response?.Data == null) {
                throw new FailedToFillException(
                    $"Failed to retrieve playlist ({Id}) page {page}: {response?.Message} ({response?.Code}).");
            }

            data = response.Data;
            Videos.AddRange(data.Medias.Select(m => $"av{m.Id}"));
        }

        Videos.Reverse();
    }
}
