using System.Collections.Generic;
using System.Linq;
using Pimix.Bilibili.BilibiliApi;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliPlaylist : DataModel {
        public const string ModelId = "bilibili/playlists";

        static PimixServiceClient<BilibiliUploader> client;

        public static PimixServiceClient<BilibiliUploader> Client =>
            client ??= new PimixServiceRestClient<BilibiliUploader>();

        public string Title { get; set; }
        public string Uploader { get; set; }

        public List<string> Videos { get; set; }

        public override void Fill() {
            var data = new PlaylistRpc().Call(Id).Data;
            Title = data.Info.Title;
            Uploader = data.Info.Upper.Name;
            Videos = data.Medias.Select(m => $"av{m.Id}").ToList();
        }
    }
}
