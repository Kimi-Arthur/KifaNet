using System;
using System.Net.Http;

namespace Kifa.Bilibili;

public class HttpClients {
    public static string BilibiliCookies { get; set; }

    public static string BiliplusCookies { get; set; }

    static HttpClient? bilibiliClient;

    public static HttpClient BilibiliHttpClient {
        get {
            if (bilibiliClient == null) {
                bilibiliClient = new HttpClient {
                    Timeout = TimeSpan.FromMinutes(10)
                };
                bilibiliClient.DefaultRequestHeaders.Add("cookie", BilibiliCookies);
                bilibiliClient.DefaultRequestHeaders.Referrer =
                    new Uri("https://space.bilibili.com/");
                bilibiliClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            }

            return bilibiliClient;
        }
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