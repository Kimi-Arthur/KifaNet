using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web;
using Newtonsoft.Json.Linq;
using NLog.Targets;
using OpenQA.Selenium.Chrome;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Cloud.Swisscom {
    public class SwisscomStorageClient : StorageClient {
        public static APIList APIList { get; set; }

        public static Dictionary<string, SwisscomAccount> Accounts { get; set; }
        public SwisscomAccount Account { get; set; }


        readonly HttpClient client = new HttpClient();

        public override long Length(string path) {
            var request = APIList.GetFileInfo.GetRequest(new Dictionary<string, string>
                {["file_id"] = GetFileId(path), ["access_token"] = Account.Token});
            using var response = client.SendAsync(request).Result;
            if (response.IsSuccessStatusCode) {
                return response.GetJToken().Value<long>("Length");
            }

            return response.StatusCode == HttpStatusCode.NotFound ? 0 : -1;
        }

        public override void Delete(string path) {
            throw new NotImplementedException();
        }

        public override void Touch(string path) {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string path) =>
            new SeekableReadStream(Length(path),
                (buffer, bufferOffset, offset, count)
                    => Download(buffer, GetFileId(path), bufferOffset, offset, count));

        public override void Write(string path, Stream stream) {
            throw new NotImplementedException();
        }


        int Download(byte[] buffer, string fileId, int bufferOffset = 0, long offset = 0,
            int count = -1) {
            if (count < 0) {
                count = buffer.Length - bufferOffset;
            }

            var request = APIList.DownloadFile.GetRequest(new Dictionary<string, string>
                {["file_id"] = fileId, ["access_token"] = Account.Token});

            request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
            using var response = client.SendAsync(request).Result;
            var memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
            response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
            return (int) memoryStream.Position;
        }

        static string GetFileId(string path) => $"/Drive{path}".ToBase64();
    }

    public class APIList {
        public API GetFileInfo { get; set; }
        public API DownloadFile { get; set; }
    }

    public class SwisscomAccount {
        public string Username { get; set; }
        public string Password { get; set; }

        string token;

        public string Token {
            get {
                if (token != null) {
                    return token;
                }

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
                return token = JToken.Parse(
                        HttpUtility.UrlDecode(driver.Manage().Cookies.GetCookieNamed("mycloud-login_token").Value))
                    .Value<string>("access_token");
            }
        }
    }
}
