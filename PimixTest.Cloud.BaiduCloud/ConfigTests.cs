using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Pimix.Cloud.BaiduCloud;
using Pimix.Service;

namespace PimixTest.Cloud.Baidu
{
    [TestClass]
    public class ConfigTests
    {
        public string PimixServerApiAddress { get; set; } = "http://test.pimix.org/api";

        [TestMethod]
        public void GetConfigTest()
        {
            DataModel.PimixServerApiAddress = PimixServerApiAddress;
            var config = DataModel.Get<BaiduCloudConfig>("baidu_cloud");
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
