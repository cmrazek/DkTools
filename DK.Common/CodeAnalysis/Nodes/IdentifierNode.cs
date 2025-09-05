using DK.Code;
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
        private Node[] _arrayAccessExps;
        private Node[] _subscriptAccessExps;
        private DataType _dataType;
        private bool _reportable;

        public IdentifierNode(Statement stmt, CodeSpan span, string name, Definition def,
            IEnumerable<Node> arrayAccessExps = null,
            IEnumerable<Node> subscriptAccessExps = null,
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

                if (_def is VariableDefinition varDef && _arrayAccessExps.Length != varDef.ArrayLengths.Length)
                {
                    scope.CodeAnalyzer.ReportError(Span, CAError.CA10180, varDef.ArrayLengths.Length, _arrayAccessExps.Length);    // Expected {0} array indexers but got {1}.
                }
            }
            else
            {
                if (_def is VariableDefinition varDef && (varDef.ArrayLengths?.Length ?? 0) > 0)
                {
                    scope.CodeAnalyzer.ReportError(Span, CAError.CA10075);  // Expected array indexer to follow variable.
                }
                else if (_def is EnumOptionDefinition && scope.IsVariable(_def.Name))
                {
                    scope.CodeAnalyzer.ReportError(Span, CAError.CA10083, _def.Name);  // Enum option '{0}' is ambigious with variable/argument of the same name.
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
                    if (v.IsInitialized != TriState.True
                        && !scope.SuppressInitializedCheck
                        && v.DataType.ValueType != ValType.Interface)
                    {
                        ReportError(Span, CAError.CA10110, v.Name);  // Use of uninitialized variable '{0}'.
                    }
                    return v.Value;
                }
                else if (_def.Name.StartsWith("$"))	// $ErrorCount
                {
                    return Value.CreateUnknownFromDataType(_def.DataType);
                }

                return base.ReadValue(scope);
            }
            else if (_def is EnumOptionDefinition)
            {
                return new EnumValue(_def.DataType, _def.Name, literal: true);
            }
            else if (_def is TableDefinition || _def is ExtractTableDefinition)
            {
                return new TableValue(_def.DataType, _def.Name, literal: true);
            }
            else if (_def is RelIndDefinition)
            {
                return new IndRelValue(_def.DataType, _def.Name, literal: true);
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

                if (_def is VariableDefinition varDef2 && _arrayAccessExps.Length != varDef2.ArrayLengths.Length)
                {
                    scope.CodeAnalyzer.ReportError(Span, CAError.CA10180, varDef2.ArrayLengths.Length, _arrayAccessExps.Length);    // Expected {0} array indexers but got {1}.
                }
            }
            else
            {
                if (_def is VariableDefinition varDef2 && (varDef2.ArrayLengths?.Length ?? 0) > 0)
                {
                    scope.CodeAnalyzer.ReportError(Span, CAError.CA10075);  // Expected array indexer to follow variable.
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

            if (_def is VariableDefinition varDef)
            {
                if (varDef.Argument && varDef.ArgumentPassByMethod == PassByMethod.Value &&
                    varDef.DataType.ValueType == ValType.String)
                {
                    ReportError(CAError.CA00106);   // Strings passed by reference are immutable; changes are not reflected back to the caller
                }

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

        public override void OnUsed(CAScope scope)
        {
            if (_def is VariableDefinition)
            {
                var v = scope.GetVariable(Text);
                if (v != null)
                {
                    v.IsUsed = true;
                }
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
