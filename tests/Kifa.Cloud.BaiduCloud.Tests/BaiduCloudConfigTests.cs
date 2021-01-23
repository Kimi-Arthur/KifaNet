using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Kifa.Cloud.BaiduCloud;
using Kifa.Service;

namespace Kifa.Cloud.BaiduCloud.Tests {
    [TestClass]
    public class BaiduCloudConfigTests {
        public string PimixServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        [TestMethod]
        public void GetConfigTest() {
            KifaServiceRestClient.ServerAddress = PimixServerApiAddress;
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
