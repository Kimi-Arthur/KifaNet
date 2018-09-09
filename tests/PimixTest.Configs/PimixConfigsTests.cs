using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pimix.Configs;
using Xunit;

namespace PimixTest.Configs {
    public class PimixConfigsTests {
        [Fact]
        public void GetAllPropertiesTest() {
            var properties = PimixConfigs.GetAllProperties();
            var keys = properties.Keys.ToList();
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.IntConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.StringConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.IntListConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.StringListConfig", keys);
            Assert.Contains("PimixTest.Configs.PimixConfigsTests.StringDictConfig", keys);
        }

        public static int IntConfig { get; set; }

        [Fact]
        public void ConfigureIntPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  IntConfig: 123";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal(123, IntConfig);
        }

        public static string StringConfig { get; set; }

        [Fact]
        public void ConfigureStringPropertyTest() {
            var properties = PimixConfigs.GetAllProperties();
            var config = @"PimixTest.Configs.PimixConfigsTests:
  StringConfig: abc";
            PimixConfigs.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(config)),
                properties);

            Assert.Equal("abc", StringConfig);
        }

        public static List<int> IntListConfig { get; set; }

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

            Assert.Equal(new List<int> {123, 233, 344}, IntListConfig);
        }

        public static List<string> StringListConfig { get; set; }

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

            Assert.Equal(new List<string> {"123", "233", "344", "abc"}, StringListConfig);
        }

        public static Dictionary<string, string> StringDictConfig { get; set; }

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
                    ["a"] = "b", ["c"] = "d", ["d"] = "e"
                },
                StringDictConfig);
        }

        public static int MultiSegmentConfig { get; set; }

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
    }
}
