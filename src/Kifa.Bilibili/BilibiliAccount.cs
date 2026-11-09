using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliAccount : DataModel, WithModelId<BilibiliAccount> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "bilibili/accounts";

    public static KifaServiceClient<BilibiliAccount> Client { get; set; } =
        new KifaServiceRestClient<BilibiliAccount>();

    public static HttpClient AuthClient {
        get => field ??= CreateAuthClient();
        set => field = value;
    }

    static HttpClient CreateAuthClient() {
        var client = new HttpClient(new HttpClientHandler {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All
        }) {
            Timeout = TimeSpan.FromMinutes(2)
        };
        client.DefaultRequestHeaders.Referrer = new Uri("https://www.bilibili.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return client;
    }

    public static string AccountId { get; set; } = "default";

    public static TimeSpan TicketRefreshInterval { get; set; } = TimeSpan.FromDays(2);

    public static TimeSpan SessDataRefreshThreshold { get; set; } = TimeSpan.FromDays(30);

    public string? Cookies { get; set; }

    public string? RefreshToken { get; set; }

    public override TimeSpan? RefreshInterval => TicketRefreshInterval;

    // -101: 账号未登录 (Account not logged in).
    // Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/misc/errcode.md
    public const int NotLoggedInCode = -101;

    // Cookie & ticket rotation references:
    // - Cookie refresh flow: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/cookie_refresh.md
    // - Web ticket (bili_ticket): https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/misc/sign/bili_ticket.md
    public override void Fill(bool deep = false) {
        Cookies ??= HttpClients.BilibiliCookies;
        if (Cookies == null) {
            throw new FailedToFillException("Cookies are not configured for BilibiliAccount.");
        }

        if (RefreshToken != null) {
            var csrf = GetCookieValue(Cookies, "bili_jct");
            var cookieInfo = AuthClient.Call(new CookieInfoRpc(csrf, Cookies));
            if (cookieInfo is { Code: NotLoggedInCode }) {
                throw new FailedToFillException(
                    $"Bilibili session is no longer logged in: {cookieInfo.Message} ({cookieInfo.Code}).");
            }

            var needSessDataRefresh = deep || cookieInfo?.Data?.Refresh == true ||
                                      ShouldRefreshSessData(Cookies);
            if (needSessDataRefresh && csrf != null) {
                var timestamp = cookieInfo?.Data?.Timestamp is > 0
                    ? cookieInfo.Data.Timestamp
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var refreshCsrf = AuthClient.Call(new CorrespondRpc(timestamp, Cookies));
                if (refreshCsrf == null) {
                    throw new FailedToFillException(
                        "Failed to obtain refresh_csrf from Bilibili correspond page.");
                }

                var refreshResult = AuthClient.Call(
                    new CookieRefreshRpc(csrf, refreshCsrf, RefreshToken, Cookies));
                if (refreshResult is not { Code: 0, Data.RefreshToken: not null }) {
                    throw new FailedToFillException(
                        $"Failed to refresh Bilibili cookies: {refreshResult?.Message} ({refreshResult?.Code}).");
                }

                var oldRefreshToken = RefreshToken;
                Cookies = MergeSetCookies(Cookies, refreshResult.SetCookies);
                RefreshToken = refreshResult.Data.RefreshToken;

                var newCsrf = GetCookieValue(Cookies, "bili_jct") ?? csrf;
                var confirmResult = AuthClient.Call(
                    new ConfirmRefreshRpc(newCsrf, oldRefreshToken, Cookies));
                if (confirmResult is { Code: not 0 }) {
                    Logger.Warn(
                        $"Confirm refresh returned non-zero code {confirmResult.Code}: {confirmResult.Message}");
                }
            }
        }

        var currentCsrf = GetCookieValue(Cookies, "bili_jct");
        var ticketResult = AuthClient.Call(new GenWebTicketRpc(currentCsrf, Cookies));
        if (ticketResult is { Code: 0, Data.Ticket: not null }) {
            var expires = ticketResult.Data.CreatedAt + ticketResult.Data.Ttl;
            Cookies = MergeCookies(Cookies, [
                ("bili_ticket", ticketResult.Data.Ticket),
                ("bili_ticket_expires", expires.ToString())
            ]);
        } else {
            Logger.Warn(
                $"Failed to refresh bili_ticket: {ticketResult?.Message} ({ticketResult?.Code})");
        }
    }

    public static bool ShouldRefreshSessData(string? cookies) {
        var expiry = GetSessDataExpiry(cookies);
        return expiry != null && expiry.Value - DateTimeOffset.UtcNow < SessDataRefreshThreshold;
    }

    public static DateTimeOffset? GetSessDataExpiry(string? cookies) {
        var sessData = GetCookieValue(cookies, "SESSDATA");
        if (sessData == null) {
            return null;
        }

        var unescaped = Uri.UnescapeDataString(sessData);
        var parts = unescaped.Split(',');
        if (parts.Length >= 2 && long.TryParse(parts[1], out var unixSeconds)) {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return null;
    }

    public static string? GetCookieValue(string? cookies, string name) {
        if (cookies == null) {
            return null;
        }

        foreach (var part in cookies.Split(';')) {
            var trimmed = part.Trim();
            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0) {
                continue;
            }

            if (trimmed[..eqIndex].Trim() == name) {
                return trimmed[(eqIndex + 1)..].Trim();
            }
        }

        return null;
    }

    public static string MergeSetCookies(string? existingCookies, IEnumerable<string> setCookies) {
        var updates = new List<(string Key, string Value)>();
        foreach (var setCookie in setCookies) {
            var firstSegment = setCookie.Split(';')[0].Trim();
            var eqIndex = firstSegment.IndexOf('=');
            if (eqIndex <= 0) {
                continue;
            }

            var key = firstSegment[..eqIndex].Trim();
            var value = firstSegment[(eqIndex + 1)..].Trim();
            if (value.Length > 0) {
                updates.Add((key, value));
            }
        }

        return MergeCookies(existingCookies, updates);
    }

    public static string MergeCookies(string? existingCookies,
        IEnumerable<(string Key, string Value)> updates) {
        var order = new List<string>();
        var map = new Dictionary<string, string>();

        if (existingCookies != null) {
            foreach (var part in existingCookies.Split(';')) {
                var trimmed = part.Trim();
                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex <= 0) {
                    continue;
                }

                var key = trimmed[..eqIndex].Trim();
                var val = trimmed[(eqIndex + 1)..].Trim();
                if (!map.ContainsKey(key)) {
                    order.Add(key);
                }

                map[key] = val;
            }
        }

        foreach (var (key, val) in updates) {
            if (!map.ContainsKey(key)) {
                order.Add(key);
            }

            map[key] = val;
        }

        return string.Join("; ", order.Select(k => $"{k}={map[k]}"));
    }
}
