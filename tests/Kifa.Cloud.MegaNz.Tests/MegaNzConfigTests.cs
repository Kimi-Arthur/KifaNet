using Kifa.Configs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kifa.Cloud.MegaNz.Tests;

[TestClass]
public class MegaNzConfigTests {
    [TestMethod]
    public void GetConfigTest() {
        KifaConfigs.Init();

        var config = MegaNzConfig.Client.Get("default");
        Assert.AreEqual("Pny3YQzV", config.Accounts["test"].Password);
        Assert.AreEqual("pimix.server+test@gmail.com", config.Accounts["test"].Username);
    }
}
