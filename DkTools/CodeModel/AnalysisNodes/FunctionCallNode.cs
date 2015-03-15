using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.AnalysisNodes
{
	class FunctionCallNode : Node
	{
		private FunctionDefinition _def;

		public FunctionCallNode(Span span, FunctionDefinition def)
			: base(span, new Value(def.DataType))
		{
			_def = def;
		}

		public override bool RequiresRValueEnumOption
		{
			get { return _def.DataType.HasEnumOptions; }
		}

		public override bool IsValidRValueEnumOption(string optionText)
		{
			return _def.DataType.IsValidEnumOption(optionText);
		}
	}
}
