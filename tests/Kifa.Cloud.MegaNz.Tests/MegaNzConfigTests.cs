using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kifa.Cloud.MegaNz;
using Kifa.Service;

namespace Kifa.Cloud.MegaNz.Tests {
    [TestClass]
    public class MegaNzConfigTests {
        public static string KifaServerApiAddress { get; set; } = "http://www.pimix.tk/api";

        [TestMethod]
        public void GetConfigTest() {
            KifaServiceRestClient.ServerAddress = KifaServerApiAddress;
            var config = MegaNzConfig.Client.Get("default");
            Assert.AreEqual("Pny3YQzV", config.Accounts["test"].Password);
            Assert.AreEqual("pimix.server+test@gmail.com", config.Accounts["test"].Username);
        }
    }
}
