using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Web;
using Kifa.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Pimix;

namespace Kifa.Cloud.Swisscom {
    public class SwisscomAccount : DataModel {
        public const string ModelId = "accounts/swisscom";

        public static string WebDriverUrl { get; set; }

        public static TimeSpan WebDriverTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public static TimeSpan PageLoadWait { get; set; } = TimeSpan.FromSeconds(3);

        static KifaServiceClient<SwisscomAccount> client;

        public static KifaServiceClient<SwisscomAccount> Client =>
            client ??= new KifaServiceRestClient<SwisscomAccount>();

        public string Username { get; set; }
        public string Password { get; set; }
        public long TotalQuota { get; set; }
        public long UsedQuota { get; set; }

        [JsonIgnore] public long LeftQuota => TotalQuota - UsedQuota;

        public long ReservedQuota { get; set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly HttpClient httpClient = new();

        string token;

        DateTime lastRefreshed = DateTime.MinValue;

        static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(4);

        public string AccessToken { get; set; }

        public override bool? Fill() {
            if (UpdateQuota().Status == KifaActionStatus.OK) {
                return false;
            }

            logger.Info("Access token expired.");

            AccessToken = GetToken();

            var result = UpdateQuota();
            if (result.Status != KifaActionStatus.OK) {
                logger.Warn($"Failed to get quota: {result}.");
            }

            return true;
        }

        string GetToken() {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            using var driver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(), WebDriverTimeout);

            return Retry.Run(() => {
                driver.Navigate().GoToUrl("https://www.mycloud.swisscom.ch/login/?response_type=code&lang=en");
                Thread.Sleep(PageLoadWait);
                driver.FindElementByCssSelector("button[data-test-id=button-use-existing-login]").Click();
                driver.FindElementById("username").SendKeys(Username);
                driver.FindElementById("continueButton").Click();
                Thread.Sleep(PageLoadWait);
                driver.FindElementById("password").SendKeys(Password);
                driver.FindElementById("submitButton").Click();
                Thread.Sleep(PageLoadWait);
                return JToken.Parse(
                        HttpUtility.UrlDecode(driver.Manage().Cookies.GetCookieNamed("mycloud-login_token").Value))
                    .Value<string>("access_token");
            }, (ex, i) => {
                if (i >= 5) {
                    throw ex;
                }

                logger.Warn(ex, $"Failed to get token for {Username}...");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            });
        }

        KifaActionResult UpdateQuota() =>
            KifaActionResult.FromAction(() => {
                using var response = httpClient.SendWithRetry(() =>
                    SwisscomStorageClient.APIList.Quota.GetRequest(
                        new Dictionary<string, string> {["access_token"] = AccessToken}));
                var data = response.GetJToken();
                UsedQuota = data.Value<long>("TotalBytes");
                TotalQuota = data.Value<long>("StorageLimit");
            });
    }
}