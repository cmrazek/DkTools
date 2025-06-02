using DK.Code;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DK.Common.Tests
{
    [TestFixture]
    internal class CodeParserTests
    {
        [Test]
        public void ReadNegativeNumber()
        {
            var code = new CodeParser("-1");
            Assert.That(code.Read(), Is.True);
            Assert.That(code.Text, Is.EqualTo("-1"));
            Assert.That(code.Type, Is.EqualTo(CodeType.Number));
        }
    }
}
