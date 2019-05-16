using System.Collections.Generic;
using Pimix.Service;
using Xunit;

namespace PimixTest.Service {
    public class DataModelTests {
        public DataModelTests() {
            PimixServiceRestClient.PimixServerApiAddress = "http://www.pimix.tk/api";
            FakeDataModel.Client.Reset();
        }

        [Fact]
        public void DataModelCallBasicTest() {
            FakeDataModel.Client.Reset();
        }

        [Fact]
        public void DataModelGetBasicTest() {
            var data = FakeDataModel.Client.Get("item0");
            Assert.Equal("item0", data.Id);
            Assert.Equal(1225, data.IntPROP);
            Assert.Equal("str prop value", data.StrProp);
            Assert.Equal(new List<string> {
                    "list prop value 1",
                    "list prop value 2",
                    ""
                },
                data.ListProp);
            Assert.Equal(new Dictionary<string, string> {
                    ["dict prop key 1"] = "dict prop value 1",
                    ["dict prop key 2"] = "dict prop value 2"
                },
                data.DictProp);
            Assert.Equal("sub prop 1 value", data.SubProp.SubProp1);
            Assert.Equal(new List<string> {
                "sub prop 2 value 1",
                "sub prop 2 value 2"
            }, data.SubProp.Sub2);
        }

        [Fact]
        public void DataModelPatchBasicTest() {
            var data = FakeDataModel.Client.Get("item0");
            Assert.Equal(1225, data.IntPROP);
            Assert.Equal("str prop value", data.StrProp);
            FakeDataModel.Client.Update(new FakeDataModel {
                IntPROP = 19910123
            }, "item0");
            FakeDataModel.Client.Update(new FakeDataModel {
                StrProp = "new str",
                Id = "item0"
            });
            data = FakeDataModel.Client.Get("item0");
            Assert.Equal(19910123, data.IntPROP);
            Assert.Equal("new str", data.StrProp);
        }
    }
}
