using NUnit.Framework;

namespace DK.Common.Tests
{
    [TestFixture]
    internal class StringHelperTests
    {
        [TestCase(5, false, 2, 28)]
        [TestCase(10, false, 2, 28)]
        [TestCase(5, true, 6, 24)]
        [TestCase(15, true, 6, 24)]
        [TestCase(2, false, 2, 28)]
        [TestCase(2, true, 6, 24)]
        [TestCase(27, false, 2, 28)]
        [TestCase(28, false, 2, 28)]
        [TestCase(27, true, 6, 24)]
        [TestCase(28, true, 6, 24)]
        [TestCase(30, false, 30, 73)]
        [TestCase(40, false, 30, 73)]
        [TestCase(72, false, 30, 73)]
        [TestCase(73, false, 30, 73)]
        [TestCase(30, true, 30, 73)]
        [TestCase(40, true, 30, 73)]
        [TestCase(72, true, 30, 73)]
        [TestCase(73, true, 30, 73)]
        [TestCase(75, false, 75, 83)]
        [TestCase(80, false, 75, 83)]
        [TestCase(82, false, 75, 83)]
        [TestCase(83, false, 75, 83)]
        [TestCase(75, true, 75, 83)]
        [TestCase(80, true, 75, 83)]
        [TestCase(82, true, 75, 83)]
        [TestCase(83, true, 75, 83)]
        public void GetSpanForLineTest(int position, bool excludeWhiteSpace, int expectedStart, int expectedEnd)
        {
            var source = @"
    this is some text.    
line with no leading or trailing whitespace
        
";
            // Line starts: 0, 2, 30, 75, 85

            var span = StringHelper.GetSpanForLine(source, position, excludeWhiteSpace);

            TestContext.Out.WriteLine($"Span: [{span.Start}] -> [{span.End}]");

            Assert.AreEqual(expectedStart, span.Start);
            Assert.AreEqual(expectedEnd, span.End);
        }
    }
}
