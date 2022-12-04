using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;

namespace Kifa.Bilibili;

public class BilibiliPlaylist : DataModel<BilibiliPlaylist> {
    public const string ModelId = "bilibili/playlists";

    static KifaServiceClient<BilibiliUploader> client;

    public static KifaServiceClient<BilibiliUploader> Client
        => client ??= new KifaServiceRestClient<BilibiliUploader>();

    public string Title { get; set; }
    public string Uploader { get; set; }

    public List<string> Videos { get; set; }

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var data = HttpClients.BilibiliHttpClient.Call(new PlaylistRpc(Id))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Failed to find playlist ({Id}).");
        }

        Title = data.Info.Title;
        Uploader = data.Info.Upper.Name;
        Videos = data.Medias.Select(m => $"av{m.Id}").ToList();
        var page = 1;
        while (data.HasMore) {
            data = HttpClients.BilibiliHttpClient.Call(new PlaylistRpc(Id, ++page))?.Data;
            if (data == null) {
                throw new DataNotFoundException($"Failed to find playlist ({Id}).");
            }

            Videos.AddRange(data.Medias.Select(m => $"av{m.Id}"));
        }

        Videos.Reverse();
        return Date.Zero;
    }
}
