using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Cloud.MegaNz;
using Pimix.Service;

namespace PimixTest.Cloud.MegaNz {
    [TestClass]
    public class MegaNzConfigTests {
        public static string PimixServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        [TestMethod]
        public void GetConfigTest() {
            KifaServiceRestClient.ServerAddress = PimixServerApiAddress;
            var config = MegaNzConfig.Client.Get("default");
            Assert.AreEqual("Pny3YQzV", config.Accounts["test"].Password);
            Assert.AreEqual("pimix.server+test@gmail.com", config.Accounts["test"].Username);
        }
    }
}
