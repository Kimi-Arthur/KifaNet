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

public class SwisscomAccount : DataModel<SwisscomAccount> {
    public const string ModelId = "accounts/swisscom";

    // TODO: find out the actual refresh duration.
    static readonly TimeSpan TokenValidDuration = TimeSpan.FromHours(10);

    public static string WebDriverUrl { get; set; }

    public static TimeSpan WebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public static TimeSpan PageLoadWait { get; set; } = TimeSpan.FromSeconds(3);

    static SwisscomAccountServiceClient client;

    public static SwisscomAccountServiceClient Client
        => client ??= new SwisscomAccountRestServiceClient();

    public string Username { get; set; }
    public string Password { get; set; }

    public string AccessToken { get; set; }

    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public override DateTimeOffset? Fill() {
        AccessToken = GetToken();
        return DateTimeOffset.UtcNow + TokenValidDuration;
    }

    string GetToken() {
        var options = new ChromeOptions();
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.114 Safari/537.36");
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

            logger.Warn(ex, $"Failed to get token for {Username}...");
            logger.Warn($"Screenshot: {driver.GetScreenshot().AsBase64EncodedString}");

            Thread.Sleep(TimeSpan.FromSeconds(5));
        });
    }
}

public interface SwisscomAccountServiceClient : KifaServiceClient<SwisscomAccount> {
}

public class SwisscomAccountRestServiceClient : KifaServiceRestClient<SwisscomAccount>,
    SwisscomAccountServiceClient {
}
