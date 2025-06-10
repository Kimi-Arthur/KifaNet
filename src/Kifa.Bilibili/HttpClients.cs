using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Kifa.Bilibili;

public class HttpClients {
    public static string? BilibiliProxy { get; set; }

    public static string BilibiliCookies { get; set; }

    static readonly Regex BiliJctPattern = new(@"bili_jct=([^;]*);");

    public static string BilibiliCsrfToken => BiliJctPattern.Match(BilibiliCookies).Groups[1].Value;

    public static string BiliplusCookies { get; set; }

    static HttpClient? bilibiliClient;

    public static HttpClient BilibiliProxiedHttpClient {
        get {
            if (bilibiliClient == null) {
                bilibiliClient = new HttpClient(new HttpClientHandler {
                    Proxy = new WebProxy(BilibiliProxy)
                });
                bilibiliClient.Timeout = TimeSpan.FromMinutes(10);
                bilibiliClient.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
                bilibiliClient.DefaultRequestHeaders.Referrer = new Uri("https://m.bilibili.com/");
                bilibiliClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return bilibiliClient;
        }
    }

    static HttpClient? bilibiliDirectClient;

    public static HttpClient BilibiliDirectHttpClient {
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

    public static HttpClient GetBilibiliClient(bool regionLocked = false) {
        return regionLocked ? BilibiliProxiedHttpClient : BilibiliDirectHttpClient;
    }

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
