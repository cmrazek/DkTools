using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DkTools;

namespace Tests
{
	[TestClass]
	public class UtilTest
	{
		[TestMethod]
		public void String_IsWord()
		{
			Assert.IsTrue("abc".IsWord());
			Assert.IsFalse("123".IsWord());
			Assert.IsFalse("1abc".IsWord());
			Assert.IsTrue("_abc".IsWord());
			Assert.IsFalse("".IsWord());
			Assert.IsFalse(((string)null).IsWord());
			Assert.IsFalse(" ".IsWord());
			Assert.IsFalse(" abc ".IsWord());
			Assert.IsFalse(" abc".IsWord());
			Assert.IsFalse("abc ".IsWord());
			Assert.IsTrue("abc123".IsWord());
			Assert.IsFalse("123abc".IsWord());
			Assert.IsTrue("ABC123".IsWord());
			Assert.IsTrue("_aBC123".IsWord());
			Assert.IsTrue("abc123___".IsWord());
			Assert.IsTrue("__ABC123__".IsWord());
		}
	}
}
