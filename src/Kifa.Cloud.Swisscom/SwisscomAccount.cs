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

        using var driver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
            WebDriverTimeout);

        return Retry.Run(() => {
            driver.Navigate()
                .GoToUrl("https://www.mycloud.swisscom.ch/login/?response_type=code&lang=en");
            Thread.Sleep(PageLoadWait);
            driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login]")
                .Click();
            driver.FindElementById("username").SendKeys(Username);
            driver.FindElementById("continueButton").Click();
            Thread.Sleep(PageLoadWait);
            driver.FindElementById("password").SendKeys(Password);
            driver.FindElementById("submitButton").Click();
            Thread.Sleep(PageLoadWait);
            var tcBoxes = driver.FindElementsById("tc-checkbox");
            if (tcBoxes.Count > 0) {
                tcBoxes[0].FindElement(By.TagName("span")).Click();
                driver.FindElementByTagName("sdx-button").Click();
                Thread.Sleep(PageLoadWait);
            }

            return JToken.Parse(
                HttpUtility.UrlDecode(driver.Manage().Cookies.GetCookieNamed("mycloud-login_token")
                    .Value)).Value<string>("access_token");
        }, (ex, i) => {
            if (i >= 5) {
                throw ex;
            }

            Logger.Warn(ex, $"Failed to get token for {Username}...");
            Logger.Warn($"Screenshot: {driver.GetScreenshot().AsBase64EncodedString}");

            Thread.Sleep(TimeSpan.FromSeconds(5));
        });
    }

    public void Register() {
        var options = GetChromeOptions();

        using var driver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
            WebDriverTimeout);
        var name = new string(Id[0], 3);

        driver.Navigate().GoToUrl("https://registration.scl.swisscom.ch/ui/reg/email-address");
        Thread.Sleep(PageLoadWait);

        var usernameField = driver.FindElementByTagName("sdx-input").GetShadowRoot()
            .FindElement(By.CssSelector("input"));
        usernameField.Clear();
        usernameField.SendKeys(Username);
        driver.FindElementByTagName("sdx-input-item").FindElement(By.CssSelector("input")).Click();

        // Code

        // Password
        while (driver.FindElementsByCssSelector("sdx-input[data-cy=password-input]").Count == 0) {
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        driver.FindElementByCssSelector("sdx-input[data-cy=password-input]").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(DefaultPassword);
        driver.FindElementByCssSelector("sdx-input[data-cy=password-repeat-input]").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(DefaultPassword);
        driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click();

        // Fill info
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("sdx-select#selectTitle").Click();
        driver.FindElementsByTagName("sdx-select-list")[0]
            .FindElements(By.CssSelector("sdx-select-option"))[0].Click();

        driver.FindElementByCssSelector("sdx-input[data-cy=firstName-input]").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(name);
        driver.FindElementByCssSelector("sdx-input[data-cy=lastName-input]").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(name);
        driver.FindElementByCssSelector("sdx-input[data-cy=birth-date-input]").GetShadowRoot()
            .FindElement(By.CssSelector("input")).SendKeys(DefaultBirthday);
        var addressInput = driver.FindElementByCssSelector("sdx-select[data-cy=address-input]")
            .GetShadowRoot().FindElement(By.CssSelector("sdx-input")).GetShadowRoot()
            .FindElement(By.CssSelector("input"));
        addressInput.Click();
        addressInput.SendKeys(DefaultAddress);
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click();

        // Finish
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("sdx-button[data-cy=continue-button]").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click();

        Thread.Sleep(PageLoadWait);

        // Register for myCloud
        driver.Navigate().GoToUrl("https://www.mycloud.swisscom.ch/login/?type=register");
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login").Click();
        Thread.Sleep(PageLoadWait);
        driver.FindElementById("username").SendKeys(Username);
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("sdx-button#continueButton").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click();

        Thread.Sleep(PageLoadWait);
        driver.FindElementById("password").SendKeys(DefaultPassword);
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("sdx-button#submitButton").GetShadowRoot()
            .FindElement(By.CssSelector("button")).Click();

        Thread.Sleep(PageLoadWait);
        foreach (var checkbox in driver.FindElementsByClassName("checkbox")) {
            checkbox.Click();
        }

        Thread.Sleep(PageLoadWait);
        Thread.Sleep(PageLoadWait);
        driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login]").Click();
        Thread.Sleep(PageLoadWait);
    }
}

public interface SwisscomAccountServiceClient : KifaServiceClient<SwisscomAccount> {
}

public class SwisscomAccountRestServiceClient : KifaServiceRestClient<SwisscomAccount>,
    SwisscomAccountServiceClient {
}
