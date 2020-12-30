using System.Collections.Generic;
using System.Linq;
using Pimix.Bilibili.BilibiliApi;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliUploader : DataModel {
        public const string ModelId = "bilibili/uploaders";

        static PimixServiceClient<BilibiliUploader> client;

        public static PimixServiceClient<BilibiliUploader> Client =>
            client ??= new PimixServiceRestClient<BilibiliUploader>();

        public string Name { get; set; }
        public List<string> Aids { get; set; }

        public override void Fill() {
            var info = new UploaderInfoRpc().Call(Id).Data;
            Name = info.Name;
            var list = new UploaderVideoRpc().Call(Id).Data.List.Vlist;
            Aids = list.OrderBy(v => v.Aid).Select(v => $"av{v.Aid}").ToList();
        }
    }
}
