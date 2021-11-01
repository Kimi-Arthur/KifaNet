using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api {
    public class PlayerRpc : JsonRpc<PlayerRpc.PlayerResponse> {
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
                skyClient.DefaultRequestHeaders.Add("cookie",
                    "_gcl_au=1.1.957693971.1630059923; _ga=GA1.3.832226358.1630059924; guuruGa=GA1.1.832226358.1630059924; _fbp=fb.1.1630059924455.357249175; SkyCultureDomain=en; __RequestVerificationToken=QsUK0hG2ceC2pgcbwR_NmcgpkB3-V_wtEry2X7HW_4fr9yOkpcKg9et1Av9nD1AIMvjnXuZmQ22Bm5StsMS7ArSB4do1; bitmovin_analytics_uuid=2ae53a24-5fce-47e3-97ad-b62df05df0a8; __zlcmid=15rkIa4bo8bBvPe; SkyEnvironment=SkySport; SL_C_23361dd035530_VID=LcQOyFMQH_; SL_C_23361dd035530_KEY=4490215b459669f2b9206a829d62d403a2ac6828; SL_C_23361dd035530_SID=NoUW3Fj-1i; SkyCookie=ARBj0OtV/eWvS7mVBopXkYb59WpZ6Ley23QqcHTzDQw=; SkyPhone=ARBj0OtV/eWvS7mVBopXkYb59WpZ6Ley23QqcHTzDQw=; ASP.NET_SessionId=tg1vzcjy5e3oc2r4rxhh3q3q; idsession=783458510; _gid=GA1.3.1626377610.1635778349; _gid=GA1.2.1626377610.1635778349; DesktopUUID=2798db9a02fb83fb7cfea5cab5066ff0; SkyNotification=1; guuruGa_gid=GA1.1.2000748001.1635778350; _ga=GA1.2.832226358.1630059924; _ga_PE5CH2MZZP=GS1.1.1635778348.56.1.1635778404.0");
                skyClient.DefaultRequestHeaders.Referrer = new Uri("https://sport.sky.ch/en/live-of-tv");
                skyClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return skyClient;
        }

        public override HttpClient HttpClient => GetClient();

        public override Dictionary<string, string> Headers { get; } = new Dictionary<string, string> {
            { "x-requested-with", "XMLHttpRequest" }
        };

        public override string UrlPattern { get; } =
            "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={id}&contentType=8";

        public PlayerResponse Call(string id) =>
            Call(new Dictionary<string, string> {
                { "id", id }
            });
    }
}
