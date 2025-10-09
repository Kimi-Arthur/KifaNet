using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Kifa.Bilibili;

public class HttpClients {
    public static string? CnProxy { get; set; }
    public static string? HkProxy { get; set; }

    public static string BilibiliCookies { get; set; }

    static readonly Regex BiliJctPattern = new(@"bili_jct=([^;]*);");

    public static string BilibiliCsrfToken => BiliJctPattern.Match(BilibiliCookies).Groups[1].Value;

    public static string BiliplusCookies { get; set; }

    static HttpClient? bilibiliCnClient;

    public static HttpClient BilibiliCnClient {
        get {
            if (bilibiliCnClient == null) {
                bilibiliCnClient = new HttpClient(new HttpClientHandler {
                    Proxy = new WebProxy(CnProxy)
                });
                bilibiliCnClient.Timeout = TimeSpan.FromMinutes(10);
                bilibiliCnClient.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
                bilibiliCnClient.DefaultRequestHeaders.Referrer =
                    new Uri("https://m.bilibili.com/");
                bilibiliCnClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return bilibiliCnClient;
        }
    }

    static HttpClient? bilibiliHkClient;

    public static HttpClient BilibiliHkClient {
        get {
            if (bilibiliHkClient == null) {
                bilibiliHkClient = new HttpClient(new HttpClientHandler {
                    Proxy = new WebProxy(HkProxy)
                });
                bilibiliHkClient.Timeout = TimeSpan.FromMinutes(10);
                bilibiliHkClient.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
                bilibiliHkClient.DefaultRequestHeaders.Referrer =
                    new Uri("https://m.bilibili.com/");
                bilibiliHkClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return bilibiliHkClient;
        }
    }

    static HttpClient? bilibiliDirectClient;

    public static HttpClient BilibiliDirectClient {
        get {
            if (bilibiliDirectClient == null) {
                bilibiliDirectClient = new HttpClient();
                bilibiliDirectClient.Timeout = TimeSpan.FromMinutes(10);
                bilibiliDirectClient.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
                bilibiliDirectClient.DefaultRequestHeaders.Referrer =
                    new Uri("https://m.bilibili.com/");
                bilibiliDirectClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return bilibiliDirectClient;
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
                biliplusClient.DefaultRequestHeaders.Add("cookie", BiliplusCookies);
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
