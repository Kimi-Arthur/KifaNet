using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Pimix.Cloud.Baidu;
using Pimix.Service;

namespace PimixTest.Cloud.Baidu
{
    [TestClass]
    public class ConfigTests
    {
        public string PimixServerApiAddress { get; set; } = "http://test.pimix.org/api";

        [TestMethod]
        public void TestGetConfig()
        {
            var config = DataModel.Get<Config>("baidu_cloud");
            Assert.IsTrue(config.ClientId.StartsWith("Tk"));
        }

        [TestMethod]
        public void TestGetConfigFromLocal()
        {
            using (StreamReader sr = new StreamReader("LocalConfig.json"))
            {
                var config = JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
                Assert.IsTrue(config.ClientId.StartsWith("Tk"));
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            DataModel.PimixServerApiAddress = PimixServerApiAddress;
        }
    }
}
