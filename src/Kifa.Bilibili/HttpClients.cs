using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class HttpClients {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string? CnProxy { get; set; }
    public static string? HkProxy { get; set; }

    public static string? BilibiliCookies { get; set; }

    public static TimeSpan CookieSyncInterval { get; set; } = TimeSpan.FromMinutes(1);

    static readonly Lock CookieLock = new();
    static DateTimeOffset lastCookieSync = Date.Zero;
    static string? activeCookies;

    public static string? GetActiveBilibiliCookies() {
        EnsureBilibiliCookies();
        return activeCookies ?? BilibiliCookies;
    }

    public static void EnsureBilibiliCookies() {
        lock (CookieLock) {
            var now = DateTimeOffset.UtcNow;
            if (activeCookies != null && now - lastCookieSync < CookieSyncInterval) {
                return;
            }

            lastCookieSync = now;
            try {
                var account = BilibiliAccount.Client.Get(BilibiliAccount.AccountId);
                if (account?.Cookies != null) {
                    activeCookies = account.Cookies;
                    return;
                }
            } catch (Exception ex) {
                Logger.Debug(ex,
                    $"Failed to fetch BilibiliAccount '{BilibiliAccount.AccountId}', using fallback cookies.");
            }

            activeCookies ??= BilibiliCookies;
        }
    }

    public static void RefreshBilibiliCookies() {
        lock (CookieLock) {
            lastCookieSync = DateTimeOffset.UtcNow;
            try {
                var account = BilibiliAccount.Client.Get(BilibiliAccount.AccountId,
                    new KifaDataOptions {
                        Refresh = true
                    });
                if (account?.Cookies != null) {
                    activeCookies = account.Cookies;
                }
            } catch (Exception ex) {
                Logger.Warn(ex,
                    $"Failed to refresh BilibiliAccount '{BilibiliAccount.AccountId}'.");
            }
        }
    }

    public static string BilibiliCsrfToken
        => BilibiliAccount.GetCookieValue(GetActiveBilibiliCookies(), "bili_jct") ?? "";

    public static string BiliplusCookies { get; set; }

    public static HttpClient BilibiliCnClient => field ??= CreateBilibiliClient(CnProxy);

    public static HttpClient BilibiliHkClient => field ??= CreateBilibiliClient(HkProxy);

    public static HttpClient BilibiliDirectClient => field ??= CreateBilibiliClient();

    static HttpClient CreateBilibiliClient(string? proxy = null) {
        var handler = new HttpClientHandler {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All
        };
        if (proxy != null) {
            handler.Proxy = new WebProxy(proxy);
        }

        var client = new HttpClient(new BilibiliCookieHandler(handler)) {
            Timeout = TimeSpan.FromMinutes(10)
        };
        client.DefaultRequestHeaders.Referrer = new Uri("https://m.bilibili.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
        return client;
    }

    class BilibiliCookieHandler : DelegatingHandler {
        public BilibiliCookieHandler(HttpMessageHandler innerHandler) : base(innerHandler) {
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            AttachCookies(request);
            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            AttachCookies(request);
            return base.SendAsync(request, cancellationToken);
        }

        static void AttachCookies(HttpRequestMessage request) {
            if (!request.Headers.Contains("cookie")) {
                var cookies = GetActiveBilibiliCookies();
                if (cookies != null) {
                    request.Headers.Add("cookie", cookies);
                }
            }
        }
    }

    public static HttpClient GetBilibiliClient(BilibiliRegion region = BilibiliRegion.Direct)
        => region switch {
            BilibiliRegion.Direct => BilibiliDirectClient,
            BilibiliRegion.Cn => BilibiliCnClient,
            BilibiliRegion.Hk => BilibiliHkClient,
            _ => throw new ArgumentOutOfRangeException(nameof(region), region,
                "region should be one of direct, cn or hk.")
        };

    static HttpClient? biliplusClient;

    public static HttpClient BiliplusHttpClient {
        get {
            if (biliplusClient == null) {
                biliplusClient = new HttpClient {
                    Timeout = TimeSpan.FromMinutes(10)
                };
                if (!string.IsNullOrEmpty(BiliplusCookies)) {
                    biliplusClient.DefaultRequestHeaders.Add("cookie", BiliplusCookies);
                }
                biliplusClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            }

            return biliplusClient;
        }
    }
}

public enum BilibiliRegion {
    Direct,
    Cn,
    Hk
}
