using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class NumberNode : TextNode
	{
		private decimal _value;

		public NumberNode(Statement stmt, Span span, string text)
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
