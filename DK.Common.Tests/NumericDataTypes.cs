using DK.Modeling;
using NUnit.Framework;

namespace DK.Common.Tests
{
    [TestFixture]
    internal class NumericDataTypes
    {
        [TestCase(1, 0, false, 9)]
        [TestCase(2, 0, false, 99)]
        [TestCase(3, 0, false, 999)]
        [TestCase(4, 0, false, 9999)]
        [TestCase(5, 0, false, 99999)]
        [TestCase(6, 0, false, 999999)]
        [TestCase(7, 0, false, 9999999)]
        [TestCase(8, 0, false, 99999999)]
        [TestCase(9, 0, false, 999999999)]
        [TestCase(5, 3, false, 99.999)]
        [TestCase(11, 2, false, 999999999.99)]
        [TestCase(1, 0, true, 9)]
        [TestCase(2, 0, true, 99)]
        [TestCase(3, 0, true, 999)]
        [TestCase(4, 0, true, 9999)]
        [TestCase(5, 0, true, 99999)]
        [TestCase(6, 0, true, 999999)]
        [TestCase(7, 0, true, 9999999)]
        [TestCase(8, 0, true, 99999999)]
        [TestCase(9, 0, true, 999999999)]
        [TestCase(5, 3, true, 99.999)]
        [TestCase(11, 2, true, 999999999.99)]
        public void NumericMax(int scale, int precision, bool signed, decimal expectedMax)
        {
            var dt = DataType.MakeNumeric(scale, precision, signed);
            Assert.That(dt.MaxNumericValue, Is.EqualTo(expectedMax));
        }

        [TestCase(1, 0, false, 0)]
        [TestCase(2, 0, false, 0)]
        [TestCase(3, 0, false, 0)]
        [TestCase(8, 0, false, 0)]
        [TestCase(9, 0, false, 0)]
        [TestCase(5, 3, false, 0)]
        [TestCase(11, 2, false, 0)]
        [TestCase(1, 0, true, -9)]
        [TestCase(2, 0, true, -99)]
        [TestCase(3, 0, true, -999)]
        [TestCase(4, 0, true, -9999)]
        [TestCase(5, 0, true, -99999)]
        [TestCase(6, 0, true, -999999)]
        [TestCase(7, 0, true, -9999999)]
        [TestCase(8, 0, true, -99999999)]
        [TestCase(9, 0, true, -999999999)]
        [TestCase(5, 3, true, -99.999)]
        [TestCase(11, 2, true, -999999999.99)]
        public void NumericMin(int scale, int precision, bool signed, decimal expectedMin)
        {
            var dt = DataType.MakeNumeric(scale, precision, signed);
            Assert.That(dt.MinNumericValue, Is.EqualTo(expectedMin));
        }

        [TestCase(1, false, "65535")]
        [TestCase(2, false, "65535")]
        [TestCase(4, false, "4294967295")]
        [TestCase(1, true, "32767")]
        [TestCase(2, true, "32767")]
        [TestCase(4, true, "2147483647")]
        public void IntegerMax(int size, bool signed, string expectedMax)
        {
            var dt = DataType.MakeInteger(size, signed);
            Assert.That(dt.MaxNumericValue, Is.EqualTo(decimal.Parse(expectedMax)));
        }

        [TestCase(1, false, "0")]
        [TestCase(2, false, "0")]
        [TestCase(4, false, "0")]
        [TestCase(1, true, "-32768")]
        [TestCase(2, true, "-32768")]
        [TestCase(4, true, "-2147483648")]
        public void IntegerMin(int size, bool signed, string expectedMin)
        {
            var dt = DataType.MakeInteger(size, signed);
            Assert.That(dt.MinNumericValue, Is.EqualTo(decimal.Parse(expectedMin)));
        }
    }
}
