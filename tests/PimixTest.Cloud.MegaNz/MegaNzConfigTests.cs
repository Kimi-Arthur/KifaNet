using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.MegaNz;

namespace PimixTest.Cloud.MegaNz
{
    [TestClass]
    public class MegaNzConfigTests
    {
        public static string PimixServerApiAddress { get; set; } = "http://pimix.cloudapp.net/api";

        [TestMethod]
        public void GetConfigTest()
        {
            MegaNzConfig.PimixServerApiAddress = PimixServerApiAddress;
            var config = MegaNzConfig.Get("default");
            Assert.AreEqual("Pny3YQzV", config.Accounts["test"].Password);
            Assert.AreEqual("pimix.server+test@gmail.com", config.Accounts["test"].Username);
        }
    }
}
