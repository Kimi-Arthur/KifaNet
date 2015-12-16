using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Pimix.Cloud.BaiduCloud;

namespace PimixTest.Cloud.BaiduCloud
{
    [TestClass]
    public class ConfigTests
    {
        public string PimixServerApiAddress { get; set; } = "http://cubie.pimix.org/api";

        [TestMethod]
        public void GetConfigTest()
        {
            BaiduCloudConfig.PimixServerApiAddress = PimixServerApiAddress;
            var config = BaiduCloudConfig.Get("baidu_cloud");
            Assert.IsTrue(config.ClientId.StartsWith("Tk"));
        }

        [TestMethod]
        public void GetConfigFromLocalTest()
        {
            using (StreamReader sr = new StreamReader("LocalConfig.json"))
            {
                var config = JsonConvert.DeserializeObject<BaiduCloudConfig>(sr.ReadToEnd());
                Assert.IsTrue(config.ClientId.StartsWith("Tk"));
            }
        }
    }
}
