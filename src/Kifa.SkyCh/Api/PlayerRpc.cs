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
                    "SkyEnvironment=SkySport; _gcl_au=1.1.1058437801.1637762047; idsession=1920960945; ASP.NET_SessionId=p3woqylbc1jkgsltotuvmjw3; _ga=GA1.3.513919764.1637762048; _gid=GA1.3.358886476.1637762048; _gat_UA-104634712-1=1; _gid=GA1.2.358886476.1637762048; _gat_UA-104634712-2=1; _fbp=fb.1.1637762048051.628363069; SkyCultureDomain=en; __RequestVerificationToken=Lzn1qqUZ5aCDeq9crdYmq8KguwTq9IbdZnNOV6PDMiwcSz55N4wGaEaXepggD4hVY_-4U290NoTYfDxxvxpoCIMxPA81; SL_C_23361dd035530_VID=4YrMnJk8cm; SL_C_23361dd035530_KEY=4490215b459669f2b9206a829d62d403a2ac6828; SL_C_23361dd035530_SID=kjOj13P6Ju; SkyCookie=ARBj0OtV/eWvS7mVBopXkYb59WpZ6Ley23QqcHTzDQw=; DesktopUUID=0c2486402e0650edb12d34f43dfc2554; SkyNotification=1; SkyPhone=ARBj0OtV/eWvS7mVBopXkYb59WpZ6Ley23QqcHTzDQw=; _ga_PE5CH2MZZP=GS1.1.1637762047.1.1.1637762075.0; _ga=GA1.1.513919764.1637762048");
                skyClient.DefaultRequestHeaders.Referrer = new Uri("https://sport.sky.ch/de/live-auf-tv");
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
