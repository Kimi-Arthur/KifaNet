using System;
using System.Threading;
using System.Web;
using Kifa.Service;
using Newtonsoft.Json.Linq;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Cloud.Swisscom;

public class SwisscomAccount : DataModel {
    public const string ModelId = "accounts/swisscom";

    static readonly TimeSpan TokenValidDuration = TimeSpan.FromDays(7);

    public static string WebDriverUrl { get; set; }

    public static TimeSpan WebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public static TimeSpan PageLoadWait { get; set; } = TimeSpan.FromSeconds(3);

    public static TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(3);
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    public static string DefaultPassword { get; set; }

    public static string DefaultBirthday { get; set; }

    public static string DefaultAddress { get; set; }

    static ChromeOptions GetChromeOptions() {
        var options = new ChromeOptions();
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");
        return options;
    }

    static SwisscomAccountServiceClient client;

    public static SwisscomAccountServiceClient Client
        => client ??= new SwisscomAccountRestServiceClient();

    public string Username { get; set; }
    public string Password { get; set; }

    public string AccessToken { get; set; }

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        AccessToken = GetToken();
        return DateTimeOffset.UtcNow + TokenValidDuration;
    }

    string GetToken() {
        var options = GetChromeOptions();
        options.AddArgument("--headless");

        return Retry.Run(() => {
            using var driver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
                WebDriverTimeout);
            try {
                driver.Navigate()
                    .GoToUrl("https://www.mycloud.swisscom.ch/login/?response_type=code&lang=en");
                Run(() => driver
                    .FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
                    .Click());
                Run(() => driver.FindElementById("username").SendKeys(Username));
                Run(() => driver.FindElementById("continueButton").Click());
                Run(() => driver.FindElementById("password").SendKeys(Password));
                Run(() => driver.FindElementById("submitButton").Click());

                try {
                    Thread.Sleep(PageLoadWait);
                    return JToken.Parse(
                            HttpUtility.UrlDecode(driver.Manage().Cookies
                                .GetCookieNamed("mycloud-login_token").Value))
                        .Value<string>("access_token");
                } catch (Exception) {
                    MaybeSkipPhone(driver);
                }

                Thread.Sleep(PageLoadWait);
                return JToken.Parse(
                        HttpUtility.UrlDecode(driver.Manage().Cookies
                            .GetCookieNamed("mycloud-login_token").Value))
                    .Value<string>("access_token");
            } catch (Exception) {
                Logger.Warn($"Screenshot: {driver.GetScreenshot().AsBase64EncodedString}");
                throw;
            }
        }, (ex, i) => {
            if (i >= 5) {
                throw ex;
            }

            Logger.Warn(ex, $"Failed to get token for {Username}...");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        });
    }

    public void Register() {
        var options = GetChromeOptions();

        using var driver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
            WebDriverTimeout);

        RegisterSwisscom(driver);

        RegisterMyCloud(driver);
    }

    void RegisterSwisscom(RemoteWebDriver driver) {
        driver.Navigate().GoToUrl("https://registration.scl.swisscom.ch/ui/reg/email-address");

        Run(() => driver.FindElementByTagName("sdx-input").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(Username));
        Run(() => driver.FindElementByTagName("sdx-input-item").FindElement(By.CssSelector("input"))
            .Click());

        // Code trigger and fill will be handled by user.
        Retry.Run(
            () => driver.FindElementByCssSelector("sdx-input[data-cy=email-code-input]")
                .GetShadowRoot().FindElement(By.CssSelector("input")).Click(), Interval,
            TimeSpan.FromMinutes(10), noLogging: true);

        // Password
        Run(() => driver.FindElementByCssSelector("sdx-input[data-cy=password-input]")
            .GetShadowRoot().FindElement(By.CssSelector("input")).SendKeys(DefaultPassword));
        Run(() => driver.FindElementByCssSelector("sdx-input[data-cy=password-repeat-input]")
            .GetShadowRoot().FindElement(By.CssSelector("input")).SendKeys(DefaultPassword));
        Run(() => driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]")
            .GetShadowRoot().FindElement(By.CssSelector("button")).Click());

        // Fill info
        Run(() => driver.FindElementByCssSelector("sdx-select#selectTitle").Click());
        Run(() => driver.FindElementsByTagName("sdx-select-list")[0]
            .FindElements(By.CssSelector("sdx-select-option"))[0].Click());

        var name = new string(Id[0], 3);
        Run(() => driver.FindElementByCssSelector("sdx-input[data-cy=firstName-input]")
            .GetShadowRoot().FindElement(By.CssSelector("input")).SendKeys(name));
        Run(() => driver.FindElementByCssSelector("sdx-input[data-cy=lastName-input]")
            .GetShadowRoot().FindElement(By.CssSelector("input")).SendKeys(name));
        Run(() => driver.FindElementByCssSelector("sdx-input[data-cy=birth-date-input]")
            .GetShadowRoot().FindElement(By.CssSelector("input")).SendKeys(DefaultBirthday));
        var addressInput = Run(()
            => driver.FindElementByCssSelector("sdx-select[data-cy=address-input]").GetShadowRoot()
                .FindElement(By.CssSelector("sdx-input")).GetShadowRoot()
                .FindElement(By.CssSelector("input")));
        Run(() => addressInput.Click());
        Run(() => addressInput.SendKeys(DefaultAddress));
        Thread.Sleep(PageLoadWait);
        Run(() => driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]")
            .GetShadowRoot().FindElement(By.CssSelector("button")).Click());

        // Finish
        Run(() => driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]")
            .GetShadowRoot().FindElement(By.CssSelector("button")).Click());
        Thread.Sleep(PageLoadWait);
    }

    void RegisterMyCloud(RemoteWebDriver driver) {
        driver.Navigate().GoToUrl("https://www.mycloud.swisscom.ch/login/?type=register");
        Run(() => driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
            .Click());
        Run(() => driver.FindElementById("username").SendKeys(Username));
        Run(() => driver.FindElementByCssSelector("sdx-button#continueButton").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click());

        Run(() => driver.FindElementById("password").SendKeys(DefaultPassword));
        Run(() => driver.FindElementByCssSelector("sdx-button#submitButton").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click());

        Thread.Sleep(PageLoadWait);
        var boxes = driver.FindElementsByClassName("checkbox");
        if (boxes.Count == 0) {
            MaybeSkipPhone(driver);
            boxes = Retry.GetItems(() => driver.FindElementsByClassName("checkbox"), Interval,
                Timeout, noLogging: true);
        }

        foreach (var checkbox in boxes) {
            Run(() => checkbox.Click());
        }

        Run(() => driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
            .Click());
        Thread.Sleep(PageLoadWait);
    }

    static void MaybeSkipPhone(RemoteWebDriver driver) {
        try {
            Retry.Run(
                () => driver
                    .FindElementByCssSelector("a[data-cy=c2f-enter-mobile-screen-skip-button]")
                    .Click(), Interval, TimeSpan.FromSeconds(30), noLogging: true);
        } catch (Exception ex) {
            Logger.Debug("No Skip element found, maybe it's fine. Ignored.");
        }
    }

    static void Run(Action action) {
        Retry.Run(action, Interval, Timeout, noLogging: true);
    }

    static T Run<T>(Func<T> action) {
        return Retry.Run<T>(action, Interval, Timeout, noLogging: true);
    }
}

public interface SwisscomAccountServiceClient : KifaServiceClient<SwisscomAccount> {
}

public class SwisscomAccountRestServiceClient : KifaServiceRestClient<SwisscomAccount>,
    SwisscomAccountServiceClient {
}
