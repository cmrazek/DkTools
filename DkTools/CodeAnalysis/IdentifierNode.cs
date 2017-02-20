using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis
{
	class IdentifierNode : TextNode
	{
		private Definition _def;

		public IdentifierNode(Statement stmt, Span span, string name, Definition def)
			: base(stmt, span, name)
		{
			_def = def;
		}

		public override bool CanAssignValue
		{
			get
			{
				return _def is VariableDefinition;
			}
		}

		public override Value Value
		{
			get
			{
				if (_def is VariableDefinition)
				{
					var value = Statement.CodeAnalyzer.GetVariable(Text);
					if (!value.Initialized) ReportError(Span, CAError.CA0009);	// Use of uninitialized value.
					return value;
				}
				else
				{
					return Value.Empty;
				}
			}
			set
			{
				if (_def is VariableDefinition)
				{
					Statement.CodeAnalyzer.SetVariable(Text, value);
				}
			}
		}
	}
}
