using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.AnalysisNodes
{
	class UnknownNode : Node
	{
		public UnknownNode(Span span)
			: base(span, Value.Void)
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
