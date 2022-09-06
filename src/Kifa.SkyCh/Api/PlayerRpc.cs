using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api;

public class PlayerRpc : JsonRpc<PlayerRpc.PlayerResponse> {
    public static string SkyChCookies { get; set; }

    public class PlayerResponse {
        public bool Success { get; set; }
        public string Url { get; set; }
        public string LicenseUrl { get; set; }
        public WatchTrackingObject WatchTrackingObject { get; set; }
        public string Language { get; set; }
        public bool DisplayResumePlayBack { get; set; }
        public bool LaunchDirectlyResumePlayBack { get; set; }
        public bool IsLive { get; set; }
        public YouboraParams YouboraParams { get; set; }
        public string PlayerTemplate { get; set; }
        public object SubsSize { get; set; }
        public object SubsColor { get; set; }
        public long NextEpisodeId { get; set; }
        public object Summary { get; set; }
    }

    public class WatchTrackingObject {
        public long CstId { get; set; }
        public long VodId { get; set; }
        public long DeviceId { get; set; }
        public long ServerId { get; set; }
        public long MovId { get; set; }
        public long CurrentPosition { get; set; }
        public long FileDuration { get; set; }
        public string DeviceKey { get; set; }
        public string Ip { get; set; }
        public string Lng { get; set; }
        public long ChannelId { get; set; }
        public long EventId { get; set; }
        public long EnvironmentId { get; set; }
        public bool IsLive { get; set; }
        public string PreferredLng { get; set; }
        public string PreferredSubsLng { get; set; }
        public long HighlightId { get; set; }
        public long VideoId { get; set; }
    }

    public class YouboraParams {
        public string AccountCode { get; set; }
        public long UserName { get; set; }
        public string Title { get; set; }
        public object Drm { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveNoSeek { get; set; }
        public string Cdn { get; set; }
        public string AppName { get; set; }
        public string Language { get; set; }
        public string[] CustomDimensions { get; set; }
        public long ContentId { get; set; }
        public object ContentTvShow { get; set; }
        public object ContentSeason { get; set; }
        public object ContentEpisodeTitle { get; set; }
        public string Channel { get; set; }
        public string Type { get; set; }
        public string PlaybackType { get; set; }
        public object Genre { get; set; }
        public string UserType { get; set; }
    }

    static HttpClient skyClient = null;

    public static HttpClient GetClient() {
        if (skyClient == null) {
            skyClient = new HttpClient {
                Timeout = TimeSpan.FromMinutes(10)
            };
            skyClient.DefaultRequestHeaders.Add("cookie", SkyChCookies);
            skyClient.DefaultRequestHeaders.Referrer =
                new Uri("https://sport.sky.ch/de/live-auf-tv");
            skyClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36");
        }

        return skyClient;
    }

    public override HttpClient HttpClient => GetClient();

    public override Dictionary<string, string> Headers { get; } = new() {
        { "x-requested-with", "XMLHttpRequest" }
    };

    public override string UrlPattern { get; } =
        "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={id}&contentType=8";

    public PlayerResponse? Invoke(string id)
        => Invoke(new Dictionary<string, string> {
            { "id", id }
        });
}
