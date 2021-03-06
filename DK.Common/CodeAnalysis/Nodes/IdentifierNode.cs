﻿using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Definitions;
using DK.Modeling;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Nodes
{
	class IdentifierNode : TextNode
	{
		private Definition _def;
		private ExpressionNode[] _arrayAccessExps;
		private ExpressionNode[] _subscriptAccessExps;
		private DataType _dataType;
		private bool _reportable;

		public IdentifierNode(Statement stmt, CodeSpan span, string name, Definition def,
			IEnumerable<ExpressionNode> arrayAccessExps = null,
			IEnumerable<ExpressionNode> subscriptAccessExps = null,
			bool reportable = true)
			: base(stmt, def.DataType, span, name)
		{
			_def = def;
			_dataType = def.DataType;
			_reportable = reportable;

			if (arrayAccessExps != null && arrayAccessExps.Any()) _arrayAccessExps = arrayAccessExps.ToArray();
			if (subscriptAccessExps != null && subscriptAccessExps.Any())
			{
				_subscriptAccessExps = subscriptAccessExps.ToArray();

				if (_dataType != null && _dataType.AllowsSubscript) _dataType = _dataType.GetSubscriptDataType(_subscriptAccessExps.Length);
			}
		}

		public override bool IsReportable { get => _reportable && _dataType != null && _dataType.IsReportable; set => _reportable = false; }
		public override string ToString() => _def.Name;

		public override bool CanAssignValue(CAScope scope)
		{
			return _def.CanWrite;
		}

		public override void Execute(CAScope scope)
		{
			// Don't read from the identifier.
		}

		public override Value ReadValue(CAScope scope)
		{
			if (_arrayAccessExps != null)
			{
				foreach (var exp in _arrayAccessExps)
				{
					var accessScope = scope.Clone();
					exp.ReadValue(accessScope);
					scope.Merge(accessScope);
				}
			}

			if (_subscriptAccessExps != null)
			{
				foreach (var exp in _subscriptAccessExps)
				{
					var accessScope = scope.Clone();
					exp.ReadValue(accessScope);
					scope.Merge(accessScope);
				}
			}

			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(Text);
				if (v != null)
				{
					v.IsUsed = true;
					if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) ReportError(Span, CAError.CA0110, v.Name);	// Use of uninitialized variable '{0}'.
					return v.Value;
				}

				return base.ReadValue(scope);
			}
			else if (_def is EnumOptionDefinition)
			{
				return new EnumValue(_def.DataType, _def.Name);
			}
			else if (_def is TableDefinition || _def is ExtractTableDefinition)
			{
				return new TableValue(_def.DataType, _def.Name);
			}
			else if (_def is RelIndDefinition)
			{
				return new IndRelValue(_def.DataType, _def.Name);
			}
			else if (_def.CanRead && _def.DataType != null)
			{
				return Value.CreateUnknownFromDataType(_def.DataType);
			}

			return base.ReadValue(scope);
		}

		public override void WriteValue(CAScope scope, Value value)
		{
			if (_arrayAccessExps != null)
			{
				foreach (var exp in _arrayAccessExps)
				{
					var accessScope = scope.Clone();
					exp.ReadValue(accessScope);
					scope.Merge(accessScope);
				}
			}

			if (_subscriptAccessExps != null)
			{
				foreach (var exp in _subscriptAccessExps)
				{
					var accessScope = scope.Clone();
					exp.ReadValue(accessScope);
					scope.Merge(accessScope);
				}
			}

			if (_def is VariableDefinition)
			{
				var v = scope.GetVariable(Text);
				if (v != null)
				{
					v.AssignValue(v.Value.Convert(scope, Span, value));
					v.IsInitialized = TriState.True;
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

		public Definition GetDefinition(CAScope scope)
		{
			return _def;
		}

		public override DataType DataType
		{
			get { return _def.DataType; }
		}
	}
}
