using DK.AppEnvironment;
using NUnit.Framework;

namespace DK.Common.Tests
{
    [TestFixture]
    public class PathUtilTests
    {
        [TestCase(@"c:\temp\log.txt", @"log.txt")]
        [TestCase(@"c:\log.txt", @"log.txt")]
        [TestCase(@"\\servername\share\info.log", @"info.log")]
        [TestCase(@"file.txt", @"file.txt")]
        [TestCase(@"X:\ccssrc1\prod\dict", @"dict")]
        [TestCase(@"X:\ccssrc1\prod\dict+", @"dict+")]
        public void GetFileNameSamples(string input, string output)
        {
            Assert.AreEqual(output, PathUtil.GetFileName(input));
        }
    }
}
