using System.Collections.Generic;
using Xunit;

namespace Kifa.Service.Tests {
    public class DataModelTests {
        public DataModelTests() {
            // FakeDataModel.Client.Reset();
        }

        [Fact]
        public void DataModelCallBasicTest() {
            FakeDataModel.Client.Reset();
        }

        [Fact]
        public void TranslateTest() {
            var x = new FakeDataModel {
                StrProp = "A",
                IntPROP = 123,
                Translations = new Dictionary<string, FakeDataModel> {
                        ["zh"] = new() {
                            StrProp = "中"
                        }
                    }
            };

            Assert.Equal("中", x.GetTranslated("zh").StrProp);
            Assert.Equal("A", x.GetTranslated("en").StrProp);
            Assert.Equal(123, x.GetTranslated("zh").IntPROP);
            Assert.Equal(123, x.GetTranslated("en").IntPROP);
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
            }, data.ListProp);
            Assert.Equal(new Dictionary<string, string> {
                ["dict prop key 1"] = "dict prop value 1",
                ["dict prop key 2"] = "dict prop value 2"
            }, data.DictProp);
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
                Id = "item0",
                IntPROP = 19910123
            });
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
