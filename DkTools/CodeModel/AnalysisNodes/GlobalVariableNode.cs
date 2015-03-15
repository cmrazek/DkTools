using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.AnalysisNodes
{
	class GlobalVariableNode : Node
	{
		private VariableDefinition _def;

		public GlobalVariableNode(Span span, VariableDefinition def)
			: base(span, Value.CreateFromDataType(def.DataType))
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
