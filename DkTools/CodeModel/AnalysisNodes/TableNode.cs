using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.AnalysisNodes
{
	class TableNode : Node
	{
		private TableDefinition _def;

		public TableNode(Span span, TableDefinition def)
			: base(span, Value.Table)
		{
			_def = def;
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
