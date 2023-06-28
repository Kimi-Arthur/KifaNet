using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class UploaderInfoRpc : KifaJsonParameterizedRpc<UploaderInfoRpc.Response> {
    #region UploadInfoRpc.Response

    public class Response {
        public long Code { get; set; }
        public string Message { get; set; }
        public long Ttl { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public long Mid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public Uri Face { get; set; }
        public string Sign { get; set; }
        public long Rank { get; set; }
        public long Level { get; set; }
        public long Jointime { get; set; }
        public long Moral { get; set; }
        public long Silence { get; set; }
        public string Birthday { get; set; }
        public long Coins { get; set; }
        public bool FansBadge { get; set; }
        public Official Official { get; set; }
        public Pendant Pendant { get; set; }
        public Nameplate Nameplate { get; set; }
        public bool IsFollowed { get; set; }
        public Uri TopPhoto { get; set; }
        public LiveRoom LiveRoom { get; set; }
    }

    public class LiveRoom {
        public long RoomStatus { get; set; }
        public long LiveStatus { get; set; }
        public Uri Url { get; set; }
        public string Title { get; set; }
        public Uri Cover { get; set; }
        public long Online { get; set; }
        public long Roomid { get; set; }
        public long RoundStatus { get; set; }
        public long BroadcastType { get; set; }
    }

    public class Nameplate {
        public long Nid { get; set; }
        public string Name { get; set; }
        public Uri Image { get; set; }
        public Uri ImageSmall { get; set; }
        public string Level { get; set; }
        public string Condition { get; set; }
    }

    public class Official {
        public long Role { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public long Type { get; set; }
    }

    public class Pendant {
        public long Pid { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public long Expire { get; set; }
        public string ImageEnhance { get; set; }
    }

    #endregion

    protected override string Url => "https://api.bilibili.com/x/space/acc/info?mid={id}";

    protected override HttpMethod Method => HttpMethod.Get;

    public UploaderInfoRpc(string uploaderId) {
        Parameters = new Dictionary<string, string> {
            { "id", uploaderId }
        };
    }
}
