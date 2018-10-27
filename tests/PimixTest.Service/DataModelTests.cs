using System.Collections.Generic;
using Pimix.Service;
using Xunit;

namespace PimixTest.Service {
    public class DataModelTests {

        public DataModelTests() {
            PimixService.PimixServerApiAddress = "http://www.pimix.tk/api";
            FakeDataModel.Reset();
        }

        [Fact]
        public void DataModelGetBasicTest() {
            var data = FakeDataModel.Get("item0");
            Assert.Equal("item0", data.Id);
            Assert.Equal(1225, data.IntProp);
            Assert.Equal("str prop value", data.StrProp);
            Assert.Equal(
                new List<string> {
                    "list prop value 1",
                    "list prop value 2",
                    ""
                },
                data.ListProp);
            Assert.Equal(
                new Dictionary<string, string> {
                    ["dict prop key 1"] = "dict prop value 1",
                    ["dict prop key 2"] = "dict prop value 2"
                },
                data.DictProp);
            Assert.Equal("sub prop 1 value", data.SubProp.SubProp1);
            Assert.Equal(
                new List<string> {
                    "sub prop 2 value 1",
                    "sub prop 2 value 2"
                }, data.SubProp.SubProp2);
        }

        [Fact]
        public void DataModelCallBasicTest() {
            FakeDataModel.Reset();
        }

        [Fact]
        public void DataModelPatchBasicTest() {
            var data = FakeDataModel.Get("item0");
            Assert.Equal(1225, data.IntProp);
            Assert.Equal("str prop value", data.StrProp);
            FakeDataModel.Patch(new FakeDataModel {IntProp = 19910123}, "item0");
            FakeDataModel.Patch(new FakeDataModel {StrProp = "new str", Id = "item0"});
            data = FakeDataModel.Get("item0");
            Assert.Equal(19910123, data.IntProp);
            Assert.Equal("new str", data.StrProp);
        }
    }
}
