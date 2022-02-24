using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Kifa.Cloud.BaiduCloud.Tests; 

[TestClass]
public class BaiduCloudConfigTests {
    [TestMethod]
    public void GetConfigTest() {
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