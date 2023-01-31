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

public class SwisscomAccount : DataModel, WithModelId {
    public static string ModelId => "swisscom/accounts";

    static readonly TimeSpan TokenValidDuration = TimeSpan.FromDays(7);

    #region public late static string WebDriverUrl { get; set; }

    static string? webDriverUrl;

    public static string WebDriverUrl {
        get => Late.Get(webDriverUrl);
        set => Late.Set(ref webDriverUrl, value);
    }

    #endregion

    public static bool NoHeadless { get; set; }

    public static TimeSpan WebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public static TimeSpan PageLoadWait { get; set; } = TimeSpan.FromSeconds(3);

    public static TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(3);
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    public static TimeSpan LongTimeout { get; set; } = TimeSpan.FromMinutes(10);

    #region public late static string DefaultPassword { get; set; }

    static string? defaultPassword;

    public static string DefaultPassword {
        get => Late.Get(defaultPassword);
        set => Late.Set(ref defaultPassword, value);
    }

    #endregion

    #region public late static string DefaultBirthday { get; set; }

    static string? defaultBirthday;

    public static string DefaultBirthday {
        get => Late.Get(defaultBirthday);
        set => Late.Set(ref defaultBirthday, value);
    }

    #endregion

    #region public late static string DefaultAddress { get; set; }

    static string? defaultAddress;

    public static string DefaultAddress {
        get => Late.Get(defaultAddress);
        set => Late.Set(ref defaultAddress, value);
    }

    #endregion

    public static KifaServiceClient<SwisscomAccount> Client { get; set; } =
        new KifaServiceRestClient<SwisscomAccount>();

    public string? Username { get; set; }
    public string? Password { get; set; }

    public string? AccessToken { get; set; }

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        if (Username == null || Password == null) {
            throw new UnableToFillException($"No account info provided for {Id}.");
        }

        AccessToken = GetRegistrationStatus().Token;
        return AccessToken != null ? DateTimeOffset.UtcNow + TokenValidDuration : Date.Zero;
    }

    public AccountRegistrationStatus Register() {
        switch (GetRegistrationStatus().Status) {
            case AccountRegistrationStatus.NotRegistered:
                RegisterSwisscom();
                Logger.Debug($"Registered account {Id} for Swisscom.");
                return GetRegistrationStatus().Status;
            case AccountRegistrationStatus.Registered:
                Logger.Debug($"Account {Id} is already fully registered.");
                return AccountRegistrationStatus.Registered;
            case AccountRegistrationStatus.OnlySwisscom:
                Logger.Debug($"Account {Id} is partially registered.");
                return AccountRegistrationStatus.OnlySwisscom;
            default:
                return AccountRegistrationStatus.NotRegistered;
        }
    }

    (AccountRegistrationStatus Status, string? Token) GetRegistrationStatus() {
        using var driver = GetDriver(true);
        driver.Navigate()
            .GoToUrl("https://www.mycloud.swisscom.ch/login/?response_type=code&lang=en");
        Run(() => driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
            .Click());
        Run(() => driver.FindElementById("username").SendKeys(Username));
        Run(() => driver.FindElementById("continueButton").Click());
        Run(() => driver.FindElementById("password").SendKeys(Password));
        Run(() => driver.FindElementById("submitButton").Click());
        Thread.Sleep(PageLoadWait);

        var url = driver.Url;
        if (url.StartsWith("https://login.prod.mdl.swisscom.ch/broker-acct-not-found")) {
            return (AccountRegistrationStatus.OnlySwisscom, null);
        }

        if (url.StartsWith("https://identity.scl.swisscom.ch/tc")) {
            var boxes =
                Retry.GetItems(() => driver.FindElementsByCssSelector("input[type=checkbox]"),
                    Interval, Timeout, noLogging: true);

            foreach (var checkbox in boxes) {
                Run(() => checkbox.Click());
            }

            Run(() => driver.FindElementByCssSelector("sdx-button[data-cy=tc-continue-button]")
                .Click());
        }

        if (url.StartsWith("https://login.mycloud.swisscom.ch/broker-terms-conditions")) {
            var boxes =
                Retry.GetItems(
                    () => driver.FindElementsByCssSelector("input[type=checkbox] + label"),
                    Interval, Timeout, noLogging: true);

            foreach (var checkbox in boxes) {
                Run(() => checkbox.Click());
            }

            Run(() => driver
                .FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
                .Click());
        }

        var errorElements = driver.FindElementsByTagName("sdx-validation-message");
        if (errorElements.Count > 0) {
            return (AccountRegistrationStatus.NotRegistered, null);
        }

        MaybeSkipPhone(driver);
        Thread.Sleep(PageLoadWait);

        if (driver.Url.StartsWith("https://login.prod.mdl.swisscom.ch/broker-acct-not-found") ||
            driver.Url.StartsWith("https://login.mycloud.swisscom.ch/broker-terms-conditions")) {
            return (AccountRegistrationStatus.OnlySwisscom, null);
        }

        return (AccountRegistrationStatus.Registered, GetToken(driver));
    }

    string GetToken(RemoteWebDriver driver)
        => GetCookieToken(driver) ?? throw new Exception(
            $"Account {Id} in an unexpected registration status. Final page ({driver.Url}): {driver.GetScreenshot().AsBase64EncodedString}");

    static string? GetCookieToken(RemoteWebDriver driver) {
        try {
            return JToken.Parse(HttpUtility.UrlDecode(driver.Manage().Cookies
                .GetCookieNamed("mycloud-login_token").Value)).Value<string>("access_token");
        } catch (NullReferenceException ex) {
            Logger.Warn("No cookie found. Maybe need to skip mobile?");
            return null;
        }
    }

    void RegisterSwisscom() {
        using var driver = GetDriver();
        driver.Navigate().GoToUrl("https://registration.scl.swisscom.ch/ui/reg/email-address");

        Run(() => driver.FindElementByTagName("sdx-input").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(Username));
        Run(() => driver.FindElementByTagName("sdx-input-item").FindElement(By.CssSelector("input"))
            .Click());
        Thread.Sleep(TimeSpan.FromSeconds(1));

        // driver.ExecuteScript(
        //     "document.querySelector('sdx-button[data-cy=continue-button]').shadowRoot.querySelector('button').focus()");
        driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click();

        // Code trigger and fill will be handled by user.
        Run(
            () => driver.FindElementByCssSelector("sdx-input[data-cy=email-code-input]")
                .GetShadowRoot().FindElement(By.CssSelector("input")).Click(), waitLong: true);

        // Password
        Run(
            () => driver.FindElementByCssSelector("sdx-input[data-cy=password-input]")
                .GetShadowRoot().FindElement(By.CssSelector("input")).SendKeys(DefaultPassword),
            waitLong: true);
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

    public KifaActionResult RegisterMyCloud()
        => KifaActionResult.FromAction(() => {
            using var driver = GetDriver(true);

            driver.Navigate().GoToUrl("https://www.mycloud.swisscom.ch/login/?type=register");
            Run(() => driver
                .FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
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
                Thread.Sleep(PageLoadWait);
                boxes = Retry.GetItems(() => driver.FindElementsByClassName("checkbox"), Interval,
                    Timeout, noLogging: true);
            }

            foreach (var checkbox in boxes) {
                Run(() => checkbox.Click());
            }

            Run(() => driver
                .FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
                .Click());
            Thread.Sleep(PageLoadWait);
        });

    static void MaybeSkipPhone(RemoteWebDriver driver) {
        try {
            Retry.Run(
                () => driver
                    .FindElementByCssSelector(
                        "a[data-cy=c2f-enter-mobile-screen-skip-button], a[data-cy=c2f-enter-mobile-screen-cancel-button]")
                    .Click(), Interval, TimeSpan.FromSeconds(30), noLogging: true);
        } catch (Exception ex) {
            Logger.Debug("No Skip element found, maybe it's fine. Ignored.");
        }
    }

    static void Run(Action action, bool waitLong = false)
        => Retry.Run(action, interval: Interval, timeout: waitLong ? LongTimeout : Timeout,
            noLogging: true);

    static T Run<T>(Func<T> action, bool waitLong = false)
        => Retry.Run(action, interval: Interval, timeout: waitLong ? LongTimeout : Timeout,
            noLogging: true) ?? throw new Exception("Failed to get element.");

    static RemoteWebDriver GetDriver(bool headless = false) {
        var options = new ChromeOptions();
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");
        if (headless && !NoHeadless) {
            options.AddArgument("--headless");
        }

        return new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
            WebDriverTimeout);
    }
}

public enum AccountRegistrationStatus {
    NotRegistered,
    OnlySwisscom,
    Registered
}
