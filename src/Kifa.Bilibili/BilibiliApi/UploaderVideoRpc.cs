using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class UploaderVideoRpc : KifaJsonParameterizedRpc<UploaderVideoRpc.Response> {
    #region UploaderVideoRpc.Response

    public class Response {
        public long Code { get; set; }
        public string Message { get; set; }
        public long Ttl { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public List List { get; set; }
        public Page Page { get; set; }
        public EpisodicButton EpisodicButton { get; set; }
    }

    public class EpisodicButton {
        public string Text { get; set; }
        public string Uri { get; set; }
    }

    public class List {
        public Dictionary<string, VideoType> Tlist { get; set; }
        public List<Video> Vlist { get; set; }
    }

    public class VideoType {
        public long Tid { get; set; }
        public long Count { get; set; }
        public string Name { get; set; }
    }

    public class Video {
        public long Comment { get; set; }

        public long Typeid { get; set; }

        // This field can be string...
        // public long Play { get; set; }
        public string Pic { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string Copyright { get; set; }
        public string Title { get; set; }
        public long Review { get; set; }
        public string Author { get; set; }
        public long Mid { get; set; }
        public long Created { get; set; }
        public string Length { get; set; }
        public long VideoReview { get; set; }
        public long Aid { get; set; }
        public string Bvid { get; set; }
        public bool HideClick { get; set; }
        public long IsPay { get; set; }
        public long IsUnionVideo { get; set; }
        public long IsSteinsGate { get; set; }
    }

    public class Page {
        public long Pn { get; set; }
        public long Ps { get; set; }
        public long Count { get; set; }
    }

    #endregion

    protected override string Url
        => "https://api.bilibili.com/x/space/arc/search?mid={id}&ps=50&pn={page}";

    protected override HttpMethod Method => HttpMethod.Get;

    public UploaderVideoRpc(string uploaderId, int page = 1) {
        Parameters = new Dictionary<string, string> {
            { "id", uploaderId },
            { "page", page.ToString() }
        };
    }
}
