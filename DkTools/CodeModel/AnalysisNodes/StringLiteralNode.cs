using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.AnalysisNodes
{
	class StringLiteralNode : Node
	{
		public StringLiteralNode(Span span, string text)
			: base(span, Value.CreateStringLiteral(text))
		{
		}

		public override bool RequiresRValueEnumOption
		{
			get { return false; }
		}

		public override bool IsValidRValueEnumOption(string optionText)
		{
			throw new InvalidOperationException();
		}
	}
}
