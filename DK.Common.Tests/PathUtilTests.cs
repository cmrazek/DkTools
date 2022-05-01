using DK.AppEnvironment;
using NUnit.Framework;
using System.IO;

namespace DK.Common.Tests
{
    [TestFixture]
    public class PathUtilTests
    {
        [TestCase(@"c:\temp\log.txt", @"log.txt")]
        [TestCase(@"c:\log.txt", @"log.txt")]
        [TestCase(@"file.txt", @"file.txt")]
        [TestCase(@"temp\file.txt", @"file.txt")]
        [TestCase(@"temp\\file.txt", @"file.txt")]
        [TestCase(@"\temp\file.txt", @"file.txt")]
        [TestCase(@"\temp\\file.txt", @"file.txt")]
        [TestCase(@"X:\ccssrc1\prod\dict", @"dict")]
        [TestCase(@"X:\ccssrc1\prod\dict+", @"dict+")]
        [TestCase(@"X:\ccssrc1\prod\", @"")]
        [TestCase(@"X:\ccssrc1\", @"")]
        [TestCase(@"X:\", @"")]
        [TestCase(@"X:", @"")]
        [TestCase(@"\\servername\share\info.log", @"info.log")]
        [TestCase(@"\\servername\share\", @"")]
        [TestCase(@"\\servername\", @"")]
        [TestCase(@"\\servername", @"servername")]
        public void GetFileName(string input, string output)
        {
            Assert.AreEqual(output, Path.GetFileName(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetFileName(input));
        }

        [TestCase(@"c:\temp\log.txt", ".txt")]
        [TestCase(@"c:\temp\log.json.txt", ".txt")]
        [TestCase(@"\\servername\share\info.log", ".log")]
        [TestCase(@"file.txt", ".txt")]
        [TestCase(@"file.txt+", ".txt+")]
        [TestCase(@"X:\ccssrc1\prod\dict", "")]
        [TestCase(@"X:\ccssrc1\prod\dict+", "")]
        [TestCase(@"X:\ccssrc1\prod\", @"")]
        [TestCase(@"X:\ccssrc1\", @"")]
        [TestCase(@"X:\ccssrc1", @"")]
        [TestCase(@"X:\", @"")]
        [TestCase(@"X:", @"")]
        [TestCase(@".gitignore", @".gitignore")]
        [TestCase(@"X:\project\.gitignore", @".gitignore")]
        public void GetExtension(string input, string output)
        {
            Assert.AreEqual(output, Path.GetExtension(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetExtension(input));
        }

        [TestCase(@"c:\temp\log.txt", "log")]
        [TestCase(@"c:\temp\log.json.txt", "log.json")]
        [TestCase(@"\\servername\share\info.log", "info")]
        [TestCase(@"file.txt", "file")]
        [TestCase(@"file.txt+", "file")]
        [TestCase(@"X:\ccssrc1\prod\dict", "dict")]
        [TestCase(@"X:\ccssrc1\prod\dict+", "dict+")]
        [TestCase(@"X:\ccssrc1\prod\", @"")]
        [TestCase(@"X:\ccssrc1\", @"")]
        [TestCase(@"X:\ccssrc1", @"ccssrc1")]
        [TestCase(@"X:\", @"")]
        [TestCase(@"X:", @"")]
        [TestCase(@".gitignore", @"")]
        [TestCase(@"X:\project\.gitignore", @"")]
        public void GetFileNameWithoutExtension(string input, string output)
        {
            Assert.AreEqual(output, Path.GetFileNameWithoutExtension(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetFileNameWithoutExtension(input));
        }

        public void GetDirectoryName(string input, string output)
        {
            Assert.AreEqual(output, Path.GetDirectoryName(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetDirectoryName(input));
        }

        [TestCase(@"X:\ccssrc1\prod", @"cust.ct", @"X:\ccssrc1\prod\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\", @"cust.ct", @"X:\ccssrc1\prod\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\", @"\cust.ct", @"\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\\", @"\cust.ct", @"\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\\", @"\cust.ct\", @"\cust.ct\")]
        [TestCase(@"X:\ccssrc1\prod\\", @"", @"X:\ccssrc1\prod\\")]
        [TestCase(@"", @"cust.ct", @"cust.ct")]
        [TestCase(@"temp", @"log.txt", @"temp\log.txt")]
        [TestCase(@"temp", @"\log.txt", @"\log.txt")]
        [TestCase(@"X:\temp\file1.txt", @"X:\temp\file2.txt", @"X:\temp\file2.txt")]
        public void CombinePath(string path1, string path2, string output)
        {
            Assert.AreEqual(output, Path.Combine(path1, path2), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.CombinePath(path1, path2));
        }

        [TestCase(@"X:\ccssrc1\prod", true)]
        [TestCase(@"\ccssrc1\prod", true)]
        [TestCase(@"ccssrc1\prod", false)]
        [TestCase(@"log.txt", false)]
        [TestCase(@"\log.txt", true)]
        [TestCase(@"\", true)]
        [TestCase(@"", false)]
        [TestCase(@"\\servername\share\info.dat", true)]
        [TestCase(@"\\servername", true)]
        [TestCase(@"X:\", true)]
        [TestCase(@"X:", true)]
        [TestCase(@"X", false)]
        public void IsPathRooted(string path, bool result)
        {
            Assert.AreEqual(result, Path.IsPathRooted(path), "Test case does not match System.IO.Path");
            Assert.AreEqual(result, PathUtil.IsPathRooted(path));
        }
    }
}
