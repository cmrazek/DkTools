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

		private bool Resolve(RunScope scope)
		{
			if (_def != null) return true;

			if (scope.DataTypeContext != null)
			{
				if (scope.DataTypeContext.HasEnumOptions && scope.DataTypeContext.IsValidEnumOption(Text))
				{
					_def = new EnumOptionDefinition(Text, scope.DataTypeContext);
					return true;
				}
			}

			var def = (from d in Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(Span.Start + scope.FuncOffset, Text)
					   where !d.RequiresChild && !d.ArgumentsRequired
					   select d).FirstOrDefault();
			if (def != null)
			{
				_def = def;
				return true;
			}

			return false;
		}

		public override bool CanAssignValue(RunScope scope)
		{
			if (_def == null && !Resolve(scope)) return false;

			return _def is VariableDefinition;
		}

		public override Value ReadValue(RunScope scope)
		{
			if (_def == null && !Resolve(scope)) return base.ReadValue(scope);

			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(Text);
				if (v != null)
				{
					if (!v.IsInitialized) ReportError(Span, CAError.CA0009);	// Use of uninitialized value.
					return v.Value;
				}

				return base.ReadValue(scope);
			}
			else if (_def.CanRead && _def.DataType != null)
			{
				return new Value(_def.DataType);
			}
			else
			{
				return base.ReadValue(scope);
			}
		}

		public override void WriteValue(RunScope scope, Value value)
		{
			if (_def == null && !Resolve(scope))
			{
				base.WriteValue(scope, value);
				return;
			}

			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(Text);
				if (v != null)
				{
					v.Value = value;
					v.IsInitialized = true;
					return;
				}

				base.WriteValue(scope, value);
			}
			else if (_def.CanWrite)
			{
			}
			else
			{
				base.WriteValue(scope, value);
			}
		}

		public Definition GetDefinition(RunScope scope)
		{
			if (_def == null && !Resolve(scope)) return null;

			return _def;
		}

		public override DataType GetDataType(RunScope scope)
		{
			if (_def == null && !Resolve(scope)) return base.GetDataType(scope);
			return _def.DataType;
		}
	}
}
