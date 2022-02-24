using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Kifa.Configs.Tests; 

public class KifaConfigsTests {
    public static int IntConfig { get; set; }

    public static string StringConfig { get; set; }

    public static List<int> IntListConfig { get; set; }

    public static List<string> StringListConfig { get; set; }

    public static Dictionary<string, string> StringDictConfig { get; set; }

    public class ComplexConfigType {
        public string S { get; set; }
        public int I { get; set; }
    }

    public static ComplexConfigType ComplexConfig { get; set; }

    public static int MultiSegmentConfig { get; set; }

    [Fact]
    public void ConfigureComplexPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests.KifaConfigsTests:
  ComplexConfig:
    I: 123
    S: AS";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal(123, ComplexConfig.I);
        Assert.Equal("AS", ComplexConfig.S);
    }

    [Fact]
    public void ConfigureIntListPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests.KifaConfigsTests:
  IntListConfig:
  - 123
  - 233
  - 344";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal(new List<int> {123, 233, 344}, IntListConfig);
    }

    [Fact]
    public void ConfigureIntPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests.KifaConfigsTests:
  IntConfig: 123";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal(123, IntConfig);
    }

    [Fact]
    public void ConfigureMultiSegmentPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests:
  KifaConfigsTests:
    MultiSegmentConfig: 123";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal(123, MultiSegmentConfig);
    }

    [Fact]
    public void ConfigureStringDictPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests.KifaConfigsTests:
  StringDictConfig:
    a: b
    c:
      d
    d: ""e""";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal(new Dictionary<string, string> {["a"] = "b", ["c"] = "d", ["d"] = "e"}, StringDictConfig);
    }

    [Fact]
    public void ConfigureStringListPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests.KifaConfigsTests:
  StringListConfig:
  - 123
  - 233
  - 344
  - abc";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal(new List<string> {"123", "233", "344", "abc"}, StringListConfig);
    }

    [Fact]
    public void ConfigureStringPropertyTest() {
        var properties = KifaConfigs.GetAllProperties();
        var config = @"Kifa.Configs.Tests.KifaConfigsTests:
  StringConfig: abc";
        KifaConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)), properties);

        Assert.Equal("abc", StringConfig);
    }

    [Fact]
    public void GetAllPropertiesTest() {
        var properties = KifaConfigs.GetAllProperties();
        var keys = properties.Keys.ToList();
        Assert.Contains("Kifa.Configs.Tests.KifaConfigsTests.IntConfig", keys);
        Assert.Contains("Kifa.Configs.Tests.KifaConfigsTests.StringConfig", keys);
        Assert.Contains("Kifa.Configs.Tests.KifaConfigsTests.IntListConfig", keys);
        Assert.Contains("Kifa.Configs.Tests.KifaConfigsTests.StringListConfig", keys);
        Assert.Contains("Kifa.Configs.Tests.KifaConfigsTests.StringDictConfig", keys);
        Assert.Contains("Kifa.Configs.Tests.KifaConfigsTests.ComplexConfig", keys);
    }
}