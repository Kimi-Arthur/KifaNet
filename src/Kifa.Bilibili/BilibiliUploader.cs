using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;

namespace Kifa.Bilibili {
    public class BilibiliUploader : DataModel {
        public const string ModelId = "bilibili/uploaders";

        static KifaServiceClient<BilibiliUploader> client;

        public static KifaServiceClient<BilibiliUploader> Client =>
            client ??= new KifaServiceRestClient<BilibiliUploader>();

        public string Name { get; set; }
        public List<string> Aids { get; set; } = new List<string>();

        public override bool? Fill() {
            var info = new UploaderInfoRpc().Call(Id).Data;
            Name = info.Name;
            var list = new UploaderVideoRpc().Call(Id).Data.List.Vlist.Select(v => v.Aid).ToHashSet();
            list.UnionWith(Aids.Select(aid => long.Parse(aid.Substring(2))).ToHashSet());
            Aids = list.OrderBy(v => v).Select(v => $"av{v}").ToList();

            return true;
        }
    }
}
