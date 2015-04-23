using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Service;

namespace PimixTest.Service
{
    [TestClass]
    public class DataModelTests
    {
        [TestMethod]
        public void DataModelGetBasicTest()
        {
            DataModel.PimixServerApiAddress = "http://cubie.pimix.org:8000/api";
            var data = DataModel.Get<FakeDataModel>("item0");
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
        public void DataModelGetIntIdTest()
        {
            DataModel.PimixServerApiAddress = "http://cubie.pimix.org:8000/api";
            var data = DataModel.Get<FakeDataModel>("item1");
            Assert.AreEqual("12", data.Id);
        }
    }
}
