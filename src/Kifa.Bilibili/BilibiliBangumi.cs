using System.Collections.Generic;
using System.Linq;
using Kifa.Bilibili.BilibiliApi;
using Pimix.Service;

namespace Kifa.Bilibili {
    public class BilibiliBangumi : DataModel {
        public const string ModelId = "bilibili/bangumis";

        static PimixServiceClient<BilibiliBangumi> client;

        public static PimixServiceClient<BilibiliBangumi> Client =>
            client ??= new PimixServiceRestClient<BilibiliBangumi>();

        public string SeasonId { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public List<string> Aids { get; set; }
        public List<string> ExtraAids { get; set; }

        public override bool Fill() {
            var mediaData = new MediaRpc().Call(Id).Result;
            SeasonId = $"ss{mediaData.Media.SeasonId}";
            Title = mediaData.Media.Title;
            Type = mediaData.Media.TypeName;
            var seasonData = new MediaSeasonRpc().Call(SeasonId).Result;
            Aids = seasonData.MainSection.Episodes.Select(e => $"av{e.Aid}").ToList();
            ExtraAids = seasonData.Section.SelectMany(s => s.Episodes.Select(e => $"av{e.Aid}")).ToList();

            return true;
        }
    }
}
