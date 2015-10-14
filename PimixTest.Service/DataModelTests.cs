using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Service;

namespace PimixTest.Service
{
    [TestClass]
    public class DataModelTests
    {
        public string PimixServerApiAddress { get; set; } = "http://test.pimix.org/api";

        [TestMethod]
        public void DataModelGetBasicTest()
        {
            var data = FakeDataModel.Get("item0");
            Assert.AreEqual("item0", data.Id);
            Assert.AreEqual(1225, data.IntProp);
            Assert.AreEqual("str prop value", data.StrProp);
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "list prop value 1",
                    "list prop value 2",
                    ""
                },
                data.ListProp);
            CollectionAssert.AreEqual(
                new Dictionary<string, string>
                {
                    ["dict prop key 1"] = "dict prop value 1",
                    ["dict prop key 2"] = "dict prop value 2"
                },
                data.DictProp);
            Assert.AreEqual("sub prop 1 value", data.SubProp.SubProp1);
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "sub prop 2 value 1",
                    "sub prop 2 value 2"
                }, data.SubProp.SubProp2);
        }

        [TestMethod]
        public void DataModelCallBasicTest()
        {
            FakeDataModel.Reset();
        }

        [TestMethod]
        public void DataModelPatchBasicTest()
        {
            var data = FakeDataModel.Get("item0");
            Assert.AreEqual(1225, data.IntProp);
            Assert.AreEqual("str prop value", data.StrProp);
            FakeDataModel.Patch(new FakeDataModel { IntProp = 19910123 }, "item0");
            FakeDataModel.Patch(new FakeDataModel { StrProp = "new str", Id = "item0" });
            data = FakeDataModel.Get("item0");
            Assert.AreEqual(19910123, data.IntProp);
            Assert.AreEqual("new str", data.StrProp);
        }

        [TestInitialize]
        public void Initialize()
        {
            FakeDataModel.PimixServerApiAddress = PimixServerApiAddress;

            FakeDataModel.Reset();
        }

        [TestCleanup]
        public void Cleanup()
        {
            FakeDataModel.Reset();
        }
    }
}
