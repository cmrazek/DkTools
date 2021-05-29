using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;

namespace DK.CodeAnalysis.Nodes
{
	abstract class Node
	{
		private Statement _stmt;
		private GroupNode _parent;
		private CodeSpan _span;
		private CAErrorType? _errorReported;
		private DataType _dataType;

		public Node(Statement stmt, DataType dataType, CodeSpan span)
		{
			_stmt = stmt;
			_dataType = dataType;
			_span = span;
		}

		public GroupNode Parent { get => _parent; set => _parent = value; }
		public CodeSpan Span { get => _span; set => _span = value; }
		public Statement Statement => _stmt;

		/// <summary>
		/// If true, this node should be written out to the report if it's the last node in a statement.
		/// </summary>
		public virtual bool IsReportable { get => false; set { } }
		public virtual int Precedence => 0;
		public virtual DataType DataType => _dataType;

		public virtual void Simplify(CAScope scope)
		{
			throw new NotImplementedException();
		}

		public virtual void Execute(CAScope scope)
		{
			ReportError(Span, CAError.CA0101);  // Syntax error.
		}

		public virtual Value ReadValue(CAScope scope)
		{
			ReportError(Span, CAError.CA0103);	// Cannot read from this identifier.
			return Value.Void;
		}

		public virtual void WriteValue(CAScope scope, Value value)
		{
			ReportError(Span, CAError.CA0102);	// Cannot write to this identifier.
		}

		public virtual bool CanAssignValue(CAScope scope)
		{
			return false;
		}

		public void ReportError(CAError errorCode, params object[] args)
		{
			ReportError(Span, errorCode, args);
		}

		public void ReportError(CodeSpan span, CAError errorCode, params object[] args)
		{
			if (_errorReported.HasValue && _errorReported.Value == CAErrorType.Error) return;

			Statement.CodeAnalyzer.ReportError(span, errorCode, args);
			_errorReported = CAErrorType.Error;
		}

		public void ReportErrorAbsolute(CodeSpan span, CAError errorCode, params object[] args)
		{
			if (_errorReported.HasValue && _errorReported.Value == CAErrorType.Error) return;

			Statement.CodeAnalyzer.ReportErrorAbsolute(span, errorCode, args);
			_errorReported = CAErrorType.Error;
		}

		public CAErrorType? ErrorReported
		{
			get { return _errorReported; }
			set { _errorReported = value; }
		}
	}
}
