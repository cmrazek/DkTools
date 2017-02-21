using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis.Nodes
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

		public override Value ReadValue(RunScope scope)
		{
			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(Text);
				if (v != null)
				{
					if (!v.IsInitialized) ReportError(Span, CAError.CA0009);	// Use of uninitialized value.
					return v.Value;
				}
			}

			return base.ReadValue(scope);
		}

		public override void WriteValue(RunScope scope, Value value)
		{
			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(Text);
				if (v != null)
				{
					v.Value = value;
					v.IsInitialized = true;
					return;
				}
			}

			base.WriteValue(scope, value);
		}

		public Definition Definition
		{
			get { return _def; }
		}
	}
}
