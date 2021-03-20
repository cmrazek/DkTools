using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	abstract class Node
	{
		private Statement _stmt;
		private GroupNode _parent;
		private CodeModel.Span _span;
		private ErrorTagging.ErrorType? _errorReported;
		private DataType _dataType;

		public Node(Statement stmt, DataType dataType, Span span)
		{
			_stmt = stmt;
			_dataType = dataType;
			_span = span;
		}

		public GroupNode Parent { get => _parent; set => _parent = value; }
		public Span Span { get => _span; set => _span = value; }
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

		public void ReportError(Span span, CAError errorCode, params object[] args)
		{
			if (_errorReported.HasValue && _errorReported.Value == ErrorTagging.ErrorType.Error) return;

			Statement.CodeAnalyzer.ReportError(span, errorCode, args);
			_errorReported = ErrorTagging.ErrorType.Error;
		}

		public void ReportErrorAbsolute(Span span, CAError errorCode, params object[] args)
		{
			if (_errorReported.HasValue && _errorReported.Value == ErrorTagging.ErrorType.Error) return;

			Statement.CodeAnalyzer.ReportErrorAbsolute(span, errorCode, args);
			_errorReported = ErrorTagging.ErrorType.Error;
		}

		public ErrorTagging.ErrorType? ErrorReported
		{
			get { return _errorReported; }
			set { _errorReported = value; }
		}
	}
}
