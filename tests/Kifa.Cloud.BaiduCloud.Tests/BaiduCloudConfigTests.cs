using System.IO;
using Kifa.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Kifa.Cloud.BaiduCloud.Tests {
    [TestClass]
    public class BaiduCloudConfigTests {
        const string KifaServerApiAddress = "http://www.pimix.tk/api";

        [TestMethod]
        public void GetConfigTest() {
            KifaServiceRestClient.ServerAddress = KifaServerApiAddress;
            var config = BaiduCloudConfig.Client.Get("default");
            Assert.IsTrue(config.Accounts.Count > 0);
        }

        [TestMethod]
        public void GetConfigFromLocalTest() {
            using var sr = new StreamReader("LocalConfig.json");
            var config = JsonConvert.DeserializeObject<BaiduCloudConfig>(sr.ReadToEnd());
            Assert.IsTrue(config.Accounts.Count > 0);
        }
    }
}
