using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;

namespace DK.CodeAnalysis.Nodes
{
	class NumberNode : TextNode
	{
		private decimal _value;

		public NumberNode(Statement stmt, CodeSpan span, string text)
			: base(stmt, DataType.Numeric, span, text)
		{
			if (!decimal.TryParse(text, out _value))
			{
#if DEBUG
				throw new InvalidOperationException(string.Format("Unable to parse number '{0}'.", text));
#endif
			}
		}

		public override void Execute(CAScope scope) { }
		public override bool IsReportable => true;
		public override Value ReadValue(CAScope scope) => new NumberValue(DataType.Numeric, _value);
		public override string ToString() => _value.ToString();
	}
}
