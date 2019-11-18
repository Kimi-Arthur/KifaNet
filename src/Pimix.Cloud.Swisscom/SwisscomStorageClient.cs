using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Web;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Chrome;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Cloud.Swisscom {
    public class SwisscomStorageClient : StorageClient {
        public static API Metadata { get; set; } = new API {
            Url = "https://storage.prod.mdl.swisscom.ch/metadata?p={file_id}",
            Method = "GET",
            Headers = new Dictionary<string, string> {
                ["Authorization"] = "Bearer {access_token}"
            }
        };

        public static Dictionary<string, SwisscomAccount> Accounts { get; set; }
        public SwisscomAccount Account { get; set; }

        public override long Length(string path) {
            var request = Metadata.GetRequest(new Dictionary<string, string>
                {["file_id"] = GetFileId(path), ["access_token"] = Account.Token});
            using var response = new HttpClient().SendAsync(request).Result;
            return response.GetJToken().Value<long>("Length");
        }

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

        string GetFileId(string path) => $"/Drive{path}".ToBase64();
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
                driver.FindElementById("username").SendKeys(Username);
                driver.FindElementById("anmelden").Click();
                driver.FindElementById("password").SendKeys(Password);
                driver.FindElementById("anmelden").Click();
                Thread.Sleep(TimeSpan.FromSeconds(2));
                return JToken.Parse(
                        HttpUtility.UrlDecode(driver.Manage().Cookies.GetCookieNamed("mycloud-login_token").Value))
                    .Value<string>("access_token");
            }
        }
    }
}
