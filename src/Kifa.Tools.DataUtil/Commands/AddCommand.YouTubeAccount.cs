using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Kifa.Service;
using Kifa.YouTube;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Tools.DataUtil.Commands;

public partial class AddCommand {
    public static string? YouTubeWebDriverUrl { get; set; }
    public static TimeSpan YouTubeWebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public static TimeSpan YouTubePageLoadWait { get; set; } = TimeSpan.FromSeconds(3);
    public static TimeSpan YouTubeInterval { get; set; } = TimeSpan.FromSeconds(3);

    // Example command: datax add -t youtube/accounts Kimily
    // Example command: datax add -t youtube/accounts Kimily:2
    // Example command: datax add -t youtube/accounts Kimily:user@gmail.com:password:1
    // Example command: datax add -t youtube/accounts Kimily:cookies.txt
    void CreateYouTubeAccounts(IEnumerable<string> specs) {
        foreach (var accountSpec in specs) {
            var segments = accountSpec.Split(":");
            var accountId = segments[0];

            YouTubeAccount account;
            var sessionCount = 1;
            string? cookieFile = null;

            if (segments.Length == 4) {
                // Spec: accountId:username:password:sessionCount
                var username = segments[1];
                var password = segments[2];
                sessionCount = int.TryParse(segments[3], out var count) ? count : 1;
                account = new YouTubeAccount {
                    Id = accountId,
                    Username = username,
                    Password = password
                };

                YouTubeAccount.Client.Update(account);
                Logger.Info($"Updated account config for {accountId}.");
            } else if (segments.Length == 2 && File.Exists(segments[1])) {
                // Spec: accountId:cookies.txt
                cookieFile = segments[1];
                account = YouTubeAccount.Client.Get(accountId) ?? new YouTubeAccount {
                    Id = accountId
                };
            } else if (segments.Length == 3 && File.Exists(segments[2])) {
                // Spec: accountId:count:cookies.txt
                sessionCount = int.TryParse(segments[1], out var count) ? count : 1;
                cookieFile = segments[2];
                account = YouTubeAccount.Client.Get(accountId) ?? new YouTubeAccount {
                    Id = accountId
                };
            } else {
                if (segments.Length > 1 && int.TryParse(segments[^1], out var parsedCount)) {
                    sessionCount = parsedCount;
                }

                account = YouTubeAccount.Client.Get(accountId) ?? new YouTubeAccount {
                    Id = accountId
                };
            }

            if (cookieFile != null) {
                var cookies = File.ReadAllText(cookieFile);
                for (var i = 0; i < sessionCount; i++) {
                    Logger.LogResult(YouTubeAccount.Client.AddSession(accountId, cookies),
                        "add new session from file", NLog.LogLevel.Info);
                }

                continue;
            }

            if (!Confirm($"Will continue to add {sessionCount} sessions to account {accountId}")) {
                continue;
            }

            for (var i = 0; i < sessionCount; i++) {
                using var driver = GetYouTubeDriver();
                driver.Navigate().GoToUrl(
                    "https://accounts.google.com/ServiceLogin?service=youtube&passive=true&continue=https%3A%2F%2Fwww.youtube.com%2F");

                if (account.Username != null) {
                    try {
                        var emailInput = Retry.Run(
                            () => driver.FindElement(
                                By.CssSelector("input[type=email], #identifierId")),
                            interval: YouTubeInterval, timeout: TimeSpan.FromSeconds(10),
                            noLogging: true);
                        if (emailInput != null) {
                            emailInput.SendKeys(account.Username);
                            var nextButton = driver.FindElement(By.Id("identifierNext"));
                            nextButton.Click();
                            Thread.Sleep(YouTubePageLoadWait);
                        }
                    } catch (Exception ex) {
                        Logger.Debug(ex, "Failed to fill in username automatically.");
                    }
                }

                Confirm(
                    $"Please complete YouTube login for account '{accountId}' in the opened browser window, then press Enter to continue...",
                    "");

                if (!driver.Url.StartsWith("https://www.youtube.com")) {
                    driver.Navigate().GoToUrl("https://www.youtube.com/");
                    Thread.Sleep(YouTubePageLoadWait);
                }

                var cookies = ToNetscapeCookieFormat(driver.Manage().Cookies.AllCookies);
                Logger.LogResult(YouTubeAccount.Client.AddSession(accountId, cookies),
                    "add new session", NLog.LogLevel.Info);
            }
        }
    }

    public static IWebDriver GetYouTubeDriver() {
        var options = new ChromeOptions();
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        if (YouTubeWebDriverUrl != null) {
            return new RemoteWebDriver(new Uri(YouTubeWebDriverUrl), options.ToCapabilities(),
                YouTubeWebDriverTimeout);
        }

        return new ChromeDriver(options);
    }

    public static string ToNetscapeCookieFormat(IEnumerable<Cookie> cookies) {
        var sb = new StringBuilder();
        sb.AppendLine("# Netscape HTTP Cookie File");
        foreach (var cookie in cookies) {
            var domain = cookie.Domain ?? "";
            var flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
            var path = cookie.Path == null || cookie.Path.Length == 0 ? "/" : cookie.Path;
            var secure = cookie.Secure ? "TRUE" : "FALSE";
            var expiry = cookie.Expiry.HasValue
                ? new DateTimeOffset(cookie.Expiry.Value).ToUnixTimeSeconds()
                : 0;
            sb.AppendLine(
                $"{domain}\t{flag}\t{path}\t{secure}\t{expiry}\t{cookie.Name}\t{cookie.Value}");
        }

        return sb.ToString();
    }
}
