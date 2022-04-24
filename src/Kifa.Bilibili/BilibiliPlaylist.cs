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

    public override DateTimeOffset? Fill() {
        var data = new PlaylistRpc().Call(Id).Data;
        Title = data.Info.Title;
        Uploader = data.Info.Upper.Name;
        Videos = data.Medias.Select(m => $"av{m.Id}").ToList();
        Videos.Reverse();

        return Date.Zero;
    }
}
