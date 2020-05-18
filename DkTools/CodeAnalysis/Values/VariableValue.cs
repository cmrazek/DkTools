using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using EnvDTE;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeAnalysis.Values
{
	/// <summary>
	/// When an assignment writes to a variable, the result is the value of the written variable,
	/// except that reads on that variable should still be reflected on the variable.
	/// This class handles that case.
	/// </summary>
	class VariableValue : Value
	{
		private VariableDefinition _def;
		private string _name;

		public VariableValue(VariableDefinition variableDefinition, string name)
			: base(variableDefinition.DataType)
		{
			_def = variableDefinition ?? throw new ArgumentNullException(nameof(variableDefinition));
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
			_name = name;
		}

		public override string ToString() => _name;

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			var v = scope.GetVariable(_name);
			if (v != null)
			{
				v.IsUsed = true;
				if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) scope.CodeAnalyzer.ReportError(span, CAError.CA0110, v.Name); // Use of uninitialized variable '{0}'.
				return v.Value.ToNumber(scope, span);
			}

			return null;
		}

		public override string ToStringValue(RunScope scope, Span span)
		{
			var v = scope.GetVariable(_name);
			if (v != null)
			{
				v.IsUsed = true;
				if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) scope.CodeAnalyzer.ReportError(span, CAError.CA0110, v.Name); // Use of uninitialized variable '{0}'.
				return v.Value.ToStringValue(scope, span);
			}

			return null;
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			var v = scope.GetVariable(_name);
			if (v != null)
			{
				v.IsUsed = true;
				if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) scope.CodeAnalyzer.ReportError(span, CAError.CA0110, v.Name); // Use of uninitialized variable '{0}'.
				return v.Value.ToDate(scope, span);
			}

			return null;
		}

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			var v = scope.GetVariable(_name);
			if (v != null)
			{
				v.IsUsed = true;
				if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) scope.CodeAnalyzer.ReportError(span, CAError.CA0110, v.Name); // Use of uninitialized variable '{0}'.
				return v.Value.ToTime(scope, span);
			}

			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			var v = scope.GetVariable(_name);
			if (v != null)
			{
				v.IsUsed = true;
				if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) scope.CodeAnalyzer.ReportError(span, CAError.CA0110, v.Name); // Use of uninitialized variable '{0}'.
				return v.Value.ToChar(scope, span);
			}

			return null;
		}

		public override Value Convert(RunScope scope, Span span, Value value)
		{
			var v = scope.GetVariable(_name);
			if (v != null)
			{
				v.IsUsed = true;
				if (v.IsInitialized != TriState.True && !scope.SuppressInitializedCheck) scope.CodeAnalyzer.ReportError(span, CAError.CA0110, v.Name); // Use of uninitialized variable '{0}'.
				return v.Value.Convert(scope, span, value);
			}

			return null;
		}
	}
}
