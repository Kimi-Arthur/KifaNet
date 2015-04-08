using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Storage;
using System.Collections.Generic;

namespace FileUtilityTests
{
    [TestClass]
    public class FileUtilityTests
    {
        [TestMethod]
        public void GetInformationFromStreamTest()
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Stream s = new MemoryStream(encoding.GetBytes("Test1"));
            var info = FileUtility.GetInformation(s, FileProperties.All);
            Assert.AreEqual(5, info.Size);
            Assert.AreEqual("E1B849F9631FFC1829B2E31402373E3C", info.MD5);
            Assert.AreEqual("99EA7BF70F6E69AD71659995677B43F8A8312025", info.SHA1);
            Assert.AreEqual("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F", info.SHA256);
            Assert.AreEqual("E1B849F9631FFC1829B2E31402373E3C", info.BlockMD5[0]);
            Assert.AreEqual("99EA7BF70F6E69AD71659995677B43F8A8312025", info.BlockSHA1[0]);
            Assert.AreEqual("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F", info.BlockSHA256[0]);

            s = new MemoryStream(encoding.GetBytes("中文长度不一样"));
            Assert.AreEqual(21, FileUtility.GetInformation(s, FileProperties.Size).Size);
        }

        [TestMethod]
        public void GetInformationFromPathTest()
        {
            var info = FileUtility.GetInformation("Test1.txt", FileProperties.All);
            Assert.AreEqual(5, info.Size);
            Assert.IsTrue(info.Path.EndsWith("/Test1.txt"), "Path value incorrect.");
            Assert.AreEqual("E1B849F9631FFC1829B2E31402373E3C", info.MD5);
            Assert.AreEqual("99EA7BF70F6E69AD71659995677B43F8A8312025", info.SHA1);
            Assert.AreEqual("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F", info.SHA256);
        }

        [TestMethod]
        public void GetInformationSomeHashesTest()
        {
            var info = FileUtility.GetInformation("Test1.txt", FileProperties.MD5 | FileProperties.SHA256);
            Assert.AreEqual("E1B849F9631FFC1829B2E31402373E3C", info.MD5);
            Assert.AreEqual(null, info.SHA1);
            Assert.AreEqual("8A863B145DC6E4ED7AC41C08F7536C476EBAC7509E028ED2B49F8BD5A3562B9F", info.SHA256);
        }

        [TestMethod]
        public void GetInformationEmpty()
        {
            var info = FileUtility.GetInformation("Test1.txt", FileProperties.None);
            Assert.IsNull(info.Path);
            Assert.IsNull(info.Size);
            Assert.IsNull(info.MD5);
            Assert.IsNull(info.SHA1);
            Assert.IsNull(info.SHA256);
            Assert.IsNull(info.BlockSize);
            Assert.IsNull(info.BlockMD5);
            Assert.IsNull(info.BlockSHA1);
            Assert.IsNull(info.BlockSHA256);
        }
    }
}
