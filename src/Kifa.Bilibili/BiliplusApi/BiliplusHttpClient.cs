using System;
using System.Net.Http;

namespace Kifa.Bilibili.BiliplusApi;

public static class BiliplusHttpClient {
    public static string BiliplusCookies { get; set; }

    static HttpClient instance;
    public static HttpClient Instance => instance ??= GetBiliplusClient();

    public static HttpClient GetBiliplusClient() {
        var client = new HttpClient {
            Timeout = TimeSpan.FromMinutes(10)
        };
        client.DefaultRequestHeaders.Add("cookie", BiliplusCookies);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
        return client;
    }
}
