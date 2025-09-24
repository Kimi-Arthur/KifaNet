using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.YouTube;

public sealed class FindYoutubeVideoRpc : KifaJsonParameterizedRpc<FindYoutubeVideoRpc.Response> {
    #region FindYoutubeVideoRpc.Response

    public class Response {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Status { get; set; }
        public Key[] Keys { get; set; }
        public Verdict Verdict { get; set; }
        public long ApiVersion { get; set; }
    }

    public class Key {
        public string Type { get; set; }
        public bool Archived { get; set; }
        public double Lastupdated { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public object Rawraw { get; set; }
        public bool Metaonly { get; set; }
        public string Classname { get; set; }
        public Available[] Available { get; set; }
        public string Suppl { get; set; }
        public string Error { get; set; }
        public bool MaybePaywalled { get; set; }
        public string KeyType { get; set; }
        public bool Comments { get; set; }
    }

    public class Available {
        public string Type { get; set; }
        public string? Url { get; set; }
        public Contains Contains { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }
        public string AvailableType { get; set; }
        public string Classname { get; set; }
    }

    public class Contains {
        public string Type { get; set; }
        public bool Video { get; set; }
        public bool Metadata { get; set; }
        public bool Comments { get; set; }
        public bool Thumbnail { get; set; }
        public bool Captions { get; set; }
        public bool StandaloneVideo { get; set; }
        public bool StandaloneAudio { get; set; }
        public bool SingleFrame { get; set; }
    }

    public class Verdict {
        public bool Video { get; set; }
        public bool Metaonly { get; set; }
        public bool Comments { get; set; }
        public string HumanFriendly { get; set; }
    }

    #endregion

    protected override string Url => "https://findyoutubevideo.thetechrobo.ca/api/v5/{id}";

    protected override HttpMethod Method => HttpMethod.Get;

    public FindYoutubeVideoRpc(string id) {
        Parameters = new() {
            { "id", id }
        };
    }
}
