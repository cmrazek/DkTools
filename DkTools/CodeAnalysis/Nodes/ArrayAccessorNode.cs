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
	class ArrayAccessorNode : Node
	{
		private Definition _def;
		private string _name;

		public ArrayAccessorNode(Statement stmt, Span span, Definition def, string name)
			: base(stmt, span)
		{
			_def = def;
			_name = name;
		}

		public override Value ReadValue(RunScope scope)
		{
			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(_name);
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
				var v = scope.GetVariable(_name);
				if (v != null)
				{
					v.Value = value;
					v.IsInitialized = true;
					return;
				}
			}

			base.WriteValue(scope, value);
		}

		public override bool CanAssignValue
		{
			get
			{
				return true;
			}
		}
	}
}
