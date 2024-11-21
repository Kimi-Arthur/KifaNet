using System;
using NLog;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Cloud.MegaNz;

public partial class MegaNzAccount {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string DefaultPassword { get; set; }
    public static string WebDriverUrl { get; set; }
    public static TimeSpan WebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public static TimeSpan PageLoadWait { get; set; } = TimeSpan.FromSeconds(3);

    public static TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(3);
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    public static TimeSpan LongTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public void Register() {
        using var driver = GetDriver();
        driver.Navigate().GoToUrl("https://mega.nz/register");
        Run(() => driver.FindElementByCssSelector("input#register-firstname-registerpage2")
            .SendKeys("first"));
        Run(() => driver.FindElementByCssSelector("input#register-lastname-registerpage2")
            .SendKeys("last"));
        Run(() => driver.FindElementByCssSelector("input#register-email-registerpage2")
            .SendKeys(Username));
        Run(() => driver.FindElementByCssSelector("input#register-password-registerpage2")
            .SendKeys(Password));
        Run(() => driver.FindElementByCssSelector("input#register-password-registerpage3")
            .SendKeys(Password));
        Run(() => driver.FindElementByCssSelector(".pw-remind input[type=checkbox]").Click());
        Run(() => driver.FindElementByCssSelector("input#register-check-registerpage2").Click());
        Run(() => driver.FindElementByCssSelector("button.register-button").Click());
        // Check email and paste link in one window
        // Auto login with the password
        // Delete description file via API
    }

    static void Run(Action action, bool waitLong = false)
        => Retry.Run(action, interval: Interval, timeout: waitLong ? LongTimeout : Timeout,
            noLogging: true);

    static RemoteWebDriver GetDriver() {
        var options = new ChromeOptions();
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");

        return new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
            WebDriverTimeout);
    }
}
