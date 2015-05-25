using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.Baidu;

namespace PimixTest.Cloud.Baidu
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void TestGetConfig()
        {
            var config = DataModel.Get<Config>("baidu_cloud");
            Assert.IsTrue(config.ClientId.startswith("Tk"));
        }
    }
}
