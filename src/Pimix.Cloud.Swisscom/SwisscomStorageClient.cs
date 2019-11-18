using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Chrome;
using Pimix.IO;

namespace Pimix.Cloud.Swisscom {
    public class SwisscomStorageClient : StorageClient {
        public static Dictionary<string, SwisscomAccount> Accounts { get; set; }
        public override long Length(string path) => throw new System.NotImplementedException();

        public override void Delete(string path) {
            throw new System.NotImplementedException();
        }

        public override void Touch(string path) {
            throw new System.NotImplementedException();
        }

        public override Stream OpenRead(string path) => throw new System.NotImplementedException();

        public override void Write(string path, Stream stream) {
            throw new System.NotImplementedException();
        }
    }

    public class SwisscomAccount {
        public string Username { get; set; }
        public string Password { get; set; }

        public string Token {
            get {
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                using var driver = new ChromeDriver(options) {
                    Url = "https://www.swisscom.ch/en/residential/mycloud/login.html",
                };
                driver.FindElementById("username").SendKeys("jingbian.ch@gmail.com");
                driver.FindElementById("anmelden").Click();
                driver.FindElementById("password").SendKeys("K2019swiss");
                driver.FindElementById("anmelden").Click();
                Thread.Sleep(TimeSpan.FromSeconds(2));
                return JToken.Parse(
                        HttpUtility.UrlDecode(driver.Manage().Cookies.GetCookieNamed("mycloud-login_token").Value))
                    .Value<string>("access_token");
            }
        }
    }
}
