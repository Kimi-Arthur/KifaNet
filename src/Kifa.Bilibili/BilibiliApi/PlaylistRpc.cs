using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class PlaylistRpc : KifaJsonParameterizedRpc<PlaylistRpc.Response> {
    #region PlaylistRpc.Response

    public class Response {
        public long Code { get; set; }
        public string Message { get; set; }
        public long Ttl { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public Info Info { get; set; }
        public List<Media> Medias { get; set; }
        public bool HasMore { get; set; }
    }

    public class Info {
        public long Id { get; set; }
        public long Fid { get; set; }
        public long Mid { get; set; }
        public long Attr { get; set; }
        public string Title { get; set; }
        public Uri Cover { get; set; }
        public InfoUpper Upper { get; set; }
        public long CoverType { get; set; }
        public InfoCntInfo CntInfo { get; set; }
        public long Type { get; set; }
        public string Intro { get; set; }
        public long Ctime { get; set; }
        public long Mtime { get; set; }
        public long State { get; set; }
        public long FavState { get; set; }
        public long LikeState { get; set; }
        public long MediaCount { get; set; }
    }

    public class InfoCntInfo {
        public long Collect { get; set; }
        public long Play { get; set; }
        public long ThumbUp { get; set; }
        public long Share { get; set; }
    }

    public class InfoUpper {
        public long Mid { get; set; }
        public string Name { get; set; }
        public Uri Face { get; set; }
        public bool Followed { get; set; }
        public long VipType { get; set; }
        public long VipStatue { get; set; }
    }

    public class Media {
        public long Id { get; set; }
        public long Type { get; set; }
        public string Title { get; set; }
        public Uri Cover { get; set; }
        public string Intro { get; set; }
        public long Page { get; set; }
        public long Duration { get; set; }
        public MediaUpper Upper { get; set; }
        public long Attr { get; set; }
        public MediaCntInfo CntInfo { get; set; }
        public string Link { get; set; }
        public long Ctime { get; set; }
        public long Pubtime { get; set; }
        public long FavTime { get; set; }
        public string Bvid { get; set; }
        public object Season { get; set; }
    }

    public class MediaCntInfo {
        public long Collect { get; set; }
        public long Play { get; set; }
        public long Danmaku { get; set; }
    }

    public class MediaUpper {
        public long Mid { get; set; }
        public string Name { get; set; }
        public Uri Face { get; set; }
    }

    #endregion

    public override string UrlPattern { get; } =
        "https://api.bilibili.com/x/v3/fav/resource/list?media_id={id}&pn={page}&ps=20";

    public override HttpMethod Method { get; } = HttpMethod.Get;

    public PlaylistRpc(string playlistId, int page = 1) {
        parameters = new Dictionary<string, string> {
            { "id", playlistId },
            { "page", page.ToString() }
        };
    }
}
