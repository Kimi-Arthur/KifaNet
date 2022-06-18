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

    static readonly TimeSpan TokenValidDuration = TimeSpan.FromDays(7);

    public static string WebDriverUrl { get; set; }

    public static TimeSpan WebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public static TimeSpan PageLoadWait { get; set; } = TimeSpan.FromSeconds(3);

    public static string DefaultPassword { get; set; }

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
        var name = Id;

        driver.Navigate().GoToUrl("https://registration.scl.swisscom.ch/ui/reg/email-address");
        Thread.Sleep(PageLoadWait);

        var usernameField = driver.FindElementByTagName("sdx-input").GetShadowRoot()
            .FindElement(By.CssSelector("input"));
        usernameField.Clear();
        usernameField.SendKeys(Username);
        driver.FindElementByTagName("sdx-input-item").GetShadowRoot()
            .FindElement(By.CssSelector("input")).Click();
        driver.FindElementById("firstName").SendKeys(name);
        driver.FindElementById("agbPart1").Click();
        driver.FindElementByCssSelector(".select__button").Click();
        driver.FindElementByCssSelector(".dropdown-item[data-value=MR]").Click();
        driver.FindElementById("submitButton").Click();
        Thread.Sleep(PageLoadWait);

        driver.FindElementById("password").SendKeys(DefaultPassword);
        driver.FindElementById("repeat-password").SendKeys(DefaultPassword);
        driver.FindElementById("captcha-input-field").Click();
        driver.FindElementById("confirmation-btn").Click();
        driver.FindElementByCssSelector(".consent-popup .button--primary").Click();

        /*
         *     email = account[1]
    driver = webdriver.Chrome()
    z = email[0] * 2
    driver.get('https://registration.scl.swisscom.ch/userinfo-xs')
    retry(lambda: driver.find_element_by_id('email').send_keys(email))
    driver.find_element_by_id('lastName').send_keys(z)
    driver.find_element_by_id('firstName').send_keys(z)
    driver.find_element_by_id('agbPart1').click()
    driver.find_elements_by_css_selector('.select__button')[0].click()
    retry(lambda: driver.find_elements_by_css_selector('.dropdown-item[data-value=MR]')[0].click())
    driver.find_element_by_id('submitButton').click()
    retry(lambda: driver.find_element_by_id('password').send_keys(password))
    driver.find_element_by_id('repeat-password').send_keys(password)
    driver.find_element_by_id('captcha-input-field').click()
    retry(lambda: driver.find_element_by_id('confirmation-btn').click())
    retry(lambda: driver.find_elements_by_css_selector('.consent-popup .button--primary')[0].click())

    driver.get('https://www.mycloud.swisscom.ch/login/?type=register')
    retry(lambda: driver.find_elements_by_css_selector('button[data-test-id=button-use-existing-login]')[0].click())
    retry(lambda: driver.find_element_by_id('username').send_keys(email))
    driver.find_element_by_id('continueButton').click()
    retry(lambda: driver.find_element_by_id('password').send_keys(password))
    driver.find_element_by_id('submitButton').click()
    retry(lambda: [box.click() for box in driver.find_elements_by_css_selector('.checkbox')])
    driver.find_elements_by_css_selector('button[data-test-id=button-use-existing-login]')[0].click()
    time.sleep(10)

    print(f'{account[0]} done.')
    driver.quit()

         */
    }
}

public interface SwisscomAccountServiceClient : KifaServiceClient<SwisscomAccount> {
}

public class SwisscomAccountRestServiceClient : KifaServiceRestClient<SwisscomAccount>,
    SwisscomAccountServiceClient {
}
