using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Pimix.Cloud.BaiduCloud;

namespace PimixTest.Cloud.BaiduCloud {
    [TestClass]
    public class BaiduCloudConfigTests {
        public string PimixServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        [TestMethod]
        public void GetConfigTest() {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerApiAddress;
            var config = BaiduCloudConfig.Get("default");
            Assert.IsTrue(config.Accounts.Count > 0);
        }

        [TestMethod]
        public void GetConfigFromLocalTest() {
            using (var sr = new StreamReader("LocalConfig.json")) {
                var config = JsonConvert.DeserializeObject<BaiduCloudConfig>(sr.ReadToEnd());
                Assert.IsTrue(config.Accounts.Count > 0);
            }
        }
    }
}
