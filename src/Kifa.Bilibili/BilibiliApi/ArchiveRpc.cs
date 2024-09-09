using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public class ArchiveRpc : KifaJsonParameterizedRpc<ArchiveRpc.Response> {
    #region ArchiveRpc.Response

    public class Response {
        public long Code { get; set; }
        public string? Message { get; set; }
        public long Ttl { get; set; }
        public Data? Data { get; set; }
    }

    public class Data {
        public List<long> Aids { get; set; } = new();
        public List<Archive> Archives { get; set; } = new();
        public Meta? Meta { get; set; }
        public Page? Page { get; set; }
    }

    public class Archive {
        public long Aid { get; set; }
        public string? Bvid { get; set; }
        public long Ctime { get; set; }
        public long Duration { get; set; }
        public bool EnableVt { get; set; }
        public bool InteractiveVideo { get; set; }
        public string? Pic { get; set; }
        public long PlaybackPosition { get; set; }
        public long Pubdate { get; set; }
        public Stat? Stat { get; set; }
        public long State { get; set; }
        public string? Title { get; set; }
        public long UgcPay { get; set; }
        public string? VtDisplay { get; set; }
    }

    public class Stat {
        public long View { get; set; }
        public long Vt { get; set; }
    }

    public class Meta {
        public long Category { get; set; }
        public string? Cover { get; set; }
        public string? Description { get; set; }
        public long Mid { get; set; }
        public string? Name { get; set; }
        public long Ptime { get; set; }
        public long SeasonId { get; set; }
        public long Total { get; set; }
    }

    public class Page {
        public long PageNum { get; set; }
        public long PageSize { get; set; }
        public long Total { get; set; }
    }

    #endregion

    protected override string Url
        => "https://api.bilibili.com/x/polymer/web-space/seasons_archives_list?mid={mid}&season_id={season_id}&sort_reverse=false&page_num={page}&page_size=30";

    protected override HttpMethod Method => HttpMethod.Get;

    public ArchiveRpc(string uploaderId, string seasonId, int page = 1) {
        Parameters = new () {
            { "mid", uploaderId },
            { "season_id", seasonId },
            { "page", page.ToString() }
        };
    }
}
