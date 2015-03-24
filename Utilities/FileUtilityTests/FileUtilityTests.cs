using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pimix.Utilities;

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
    }
}
