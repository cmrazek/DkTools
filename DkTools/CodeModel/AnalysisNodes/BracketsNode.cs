using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.AnalysisNodes
{
	class BracketsNode : Node
	{
		public BracketsNode(Span span)
			: base(span, Value.Unknown)
		{ }

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
