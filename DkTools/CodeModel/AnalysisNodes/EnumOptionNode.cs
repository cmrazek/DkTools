using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.AnalysisNodes
{
	class EnumOptionNode : Node
	{
		public EnumOptionNode(Span span, string optionText)
			: base(span, Value.CreateEnum(optionText))
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
