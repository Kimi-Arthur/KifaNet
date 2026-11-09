using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Kifa.Bilibili;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using OpenQA.Selenium;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    public static TimeSpan BilibiliLoginTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public static TimeSpan BilibiliPollInterval { get; set; } = TimeSpan.FromSeconds(3);

    public const string BilibiliAccountSpecHelp =
        "bilibili/accounts: <id> (browser) | <id>:qr | <id>:<ac_time_value|cookie_file> | <id>:<cookie_file>:<ac_time_value>";

    // Example command: datax add -t bilibili/accounts default
    // Example command: datax add -t bilibili/accounts default:qr
    // Example command: datax add -t bilibili/accounts default:ac_time_value
    // Example command: datax add -t bilibili/accounts default:cookies.txt:ac_time_value
    void CreateBilibiliAccounts(IEnumerable<string> specs) {
        foreach (var accountSpec in specs) {
            var segments = accountSpec.Split(":", 3);
            var accountId = segments[0];

            var account = BilibiliAccount.Client.Get(accountId) ?? new BilibiliAccount {
                Id = accountId,
                Cookies = HttpClients.BilibiliCookies
            };

            if (segments.Length == 3) {
                account.Cookies = File.Exists(segments[1])
                    ? File.ReadAllText(segments[1]).Trim()
                    : segments[1];
                account.RefreshToken = segments[2];
                Logger.LogResult(BilibiliAccount.Client.Set(account),
                    $"save Bilibili account {accountId}", NLog.LogLevel.Info);
                continue;
            }

            if (segments.Length == 2 && segments[1] != "qr") {
                if (File.Exists(segments[1])) {
                    account.Cookies = File.ReadAllText(segments[1]).Trim();
                } else {
                    account.Cookies ??= HttpClients.BilibiliCookies;
                    account.RefreshToken = segments[1];
                }

                Logger.LogResult(BilibiliAccount.Client.Set(account),
                    $"save Bilibili account {accountId}", NLog.LogLevel.Info);
                continue;
            }

            if (segments.Length == 2 && segments[1] == "qr") {
                LoginBilibiliWithQrCode(account);
                continue;
            }

            if (!Confirm($"Will open browser to log in Bilibili account '{accountId}'")) {
                continue;
            }

            using var driver = GetYouTubeDriver();
            driver.Navigate().GoToUrl("https://passport.bilibili.com/login");

            Logger.Info(
                $"Waiting for Bilibili login to complete for account '{accountId}' (will auto-detect once logged in)...");

            var deadline = DateTime.UtcNow + BilibiliLoginTimeout;
            while (DateTime.UtcNow < deadline) {
                try {
                    if (IsBilibiliLoggedIn(driver)) {
                        Logger.Info($"Detected successful Bilibili login for '{accountId}'.");
                        break;
                    }
                } catch (WebDriverException) {
                }

                Thread.Sleep(BilibiliPollInterval);
            }

            if (!driver.Url.StartsWith("https://www.bilibili.com")) {
                driver.Navigate().GoToUrl("https://www.bilibili.com/");
                Thread.Sleep(BilibiliPollInterval);
            }

            var cookieUpdates = driver.Manage().Cookies.AllCookies
                .Select(c => (c.Name, c.Value)).ToList();
            account.Cookies = BilibiliAccount.MergeCookies(account.Cookies, cookieUpdates);

            try {
                if (driver is IJavaScriptExecutor jsExecutor) {
                    var acTimeValue =
                        jsExecutor.ExecuteScript(
                            "return window.localStorage.getItem('ac_time_value');") as string;
                    if (acTimeValue != null) {
                        account.RefreshToken = acTimeValue;
                    }
                }
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to read ac_time_value from localStorage.");
            }

            Logger.LogResult(BilibiliAccount.Client.Set(account),
                $"save Bilibili account {accountId}", NLog.LogLevel.Info);
        }
    }

    static void LoginBilibiliWithQrCode(BilibiliAccount account) {
        var generateResponse = BilibiliAccount.AuthClient.Call(new QrCodeGenerateRpc());
        if (generateResponse is not { Code: 0, Data: { Url: not null, QrcodeKey: not null } }) {
            Logger.Error(
                $"Failed to generate Bilibili QR code: {generateResponse?.Message} ({generateResponse?.Code})");
            return;
        }

        Logger.Info(
            $"Scan this QR code URL with the Bilibili app to log in '{account.Id}':\n{generateResponse.Data.Url}");

        var deadline = DateTime.UtcNow + BilibiliLoginTimeout;
        while (DateTime.UtcNow < deadline) {
            Thread.Sleep(BilibiliPollInterval);
            var pollResponse = BilibiliAccount.AuthClient.Call(
                new QrCodePollRpc(generateResponse.Data.QrcodeKey));
            if (pollResponse?.Data == null) {
                continue;
            }

            if (pollResponse.Data.Code == 0) {
                account.Cookies = BilibiliAccount.MergeSetCookies(account.Cookies,
                    pollResponse.SetCookies);
                account.RefreshToken = pollResponse.Data.RefreshToken;
                Logger.Info($"QR code login succeeded for '{account.Id}'.");
                Logger.LogResult(BilibiliAccount.Client.Set(account),
                    $"save Bilibili account {account.Id}", NLog.LogLevel.Info);
                return;
            }

            if (pollResponse.Data.Code == QrCodePollRpc.PollData.ExpiredCode) {
                Logger.Warn("QR code expired.");
                return;
            }
        }

        Logger.Warn("Timed out waiting for Bilibili QR code login.");
    }

    static bool IsBilibiliLoggedIn(IWebDriver driver) {
        var url = driver.Url;
        if (url.Contains("bilibili.com")) {
            var cookies = driver.Manage().Cookies.AllCookies;
            return cookies.Any(c => c.Name == "SESSDATA" && !string.IsNullOrEmpty(c.Value));
        }

        return false;
    }
}
