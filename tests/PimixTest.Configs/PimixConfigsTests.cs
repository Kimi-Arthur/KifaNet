using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pimix.Configs;
using Xunit;

namespace PimixTest.Configs {
    public class PimixConfigsTests {
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
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  ComplexConfig:
    I: 123
    S: AS";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(123, ComplexConfig.I);
            Assert.Equal("AS", ComplexConfig.S);
        }

        [Fact]
        public void ConfigureIntListPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  IntListConfig:
  - 123
  - 233
  - 344";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(new List<int> {
                123,
                233,
                344
            }, IntListConfig);
        }

        [Fact]
        public void ConfigureIntPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  IntConfig: 123";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(123, IntConfig);
        }

        [Fact]
        public void ConfigureMultiSegmentPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs:
  PimixConfigsTests:
    MultiSegmentConfig: 123";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(123, MultiSegmentConfig);
        }

        [Fact]
        public void ConfigureStringDictPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  StringDictConfig:
    a: b
    c:
      d
    d: ""e""";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(new Dictionary<string, string> {
                    ["a"] = "b",
                    ["c"] = "d",
                    ["d"] = "e"
                },
                StringDictConfig);
        }

        [Fact]
        public void ConfigureStringListPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  StringListConfig:
  - 123
  - 233
  - 344
  - abc";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(new List<string> {
                "123",
                "233",
                "344",
                "abc"
            }, StringListConfig);
        }

        [Fact]
        public void ConfigureStringPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  StringConfig: abc";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal("abc", StringConfig);
        }

        [Fact]
        public void GetAllPropertiesTest() {
            var properties = PimixConfigs.GetAllProperties();
            var keys = properties.Keys.ToList();
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.IntConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.StringConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.IntListConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.StringListConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.StringDictConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.ComplexConfig", keys);
        }
    }
}
