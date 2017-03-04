using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis.Nodes
{
	class IdentifierNode : TextNode
	{
		private Definition _def;
		private ExpressionNode[] _arrayAccessExps;
		private ExpressionNode[] _subscriptAccessExps;
		private DataType _dataType;

		public IdentifierNode(Statement stmt, Span span, string name, Definition def, IEnumerable<ExpressionNode> arrayAccessExps = null,
			IEnumerable<ExpressionNode> subscriptAccessExps = null)
			: base(stmt, span, name)
		{
			_def = def;
			_dataType = def != null ? def.DataType : null;

			if (arrayAccessExps != null && arrayAccessExps.Any()) _arrayAccessExps = arrayAccessExps.ToArray();
			if (subscriptAccessExps != null && subscriptAccessExps.Any())
			{
				_subscriptAccessExps = subscriptAccessExps.ToArray();

				if (_dataType != null && _dataType.AllowsSubscript) _dataType = _dataType.GetSubscriptDataType(_subscriptAccessExps.Length);
			}
		}

		private bool Resolve(RunScope scope)
		{
			if (_def != null) return true;

			if (scope.DataTypeContext != null)
			{
				if (scope.DataTypeContext.HasEnumOptions && scope.DataTypeContext.IsValidEnumOption(Text))
				{
					_def = new EnumOptionDefinition(Text, scope.DataTypeContext);
					_dataType = scope.DataTypeContext;
					return true;
				}
			}

			var def = (from d in Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(Span.Start + scope.FuncOffset, Text)
					   where !d.RequiresChild && !d.ArgumentsRequired
					   select d).FirstOrDefault();
			if (def != null)
			{
				_def = def;
				_dataType = def.DataType;
				return true;
			}

			return false;
		}

		public override bool CanAssignValue(RunScope scope)
		{
			if (_def == null && !Resolve(scope)) return false;

			return _def.CanWrite;
		}

		public override Value ReadValue(RunScope scope)
		{
			if (_def == null && !Resolve(scope)) return base.ReadValue(scope);

			if (_arrayAccessExps != null)
			{
				foreach (var exp in _arrayAccessExps)
				{
					var accessScope = scope.Clone(dataTypeContext: DataType.Int);
					exp.ReadValue(accessScope);
					scope.Merge(accessScope);
				}
			}

			if (_subscriptAccessExps != null)
			{
				foreach (var exp in _subscriptAccessExps)
				{
					var accessScope = scope.Clone(dataTypeContext: DataType.Int);
					exp.ReadValue(accessScope);
					scope.Merge(accessScope);
				}
			}

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
			else if (_def is EnumOptionDefinition)
			{
				return new EnumValue(_def.DataType, _def.Name);
			}
			else if (_def.CanRead && _def.DataType != null)
			{
				return Value.CreateUnknownFromDataType(_def.DataType);
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
			return _dataType;
		}
	}
}
