using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	abstract class Node
	{
		private Statement _stmt;
		private GroupNode _parent;
		private CodeModel.Span _span;
		private ErrorTagging.ErrorType? _errorReported;

		public Node(Statement stmt, CodeModel.Span span)
		{
			_stmt = stmt;
			_span = span;
		}

		public Statement Statement
		{
			get { return _stmt; }
		}

		public GroupNode Parent
		{
			get { return _parent; }
			set { _parent = value; }
		}

		public Span Span
		{
			get { return _span; }
			set { _span = value; }
		}

		public virtual void Execute()
		{
		}

		public virtual int Precedence
		{
			get { return 0; }
		}

		public virtual Value Value
		{
			get { return Value.Empty; }
			set { }
		}

		public virtual bool CanAssignValue
		{
			get { return false; }
		}

		public void ReportError(Span span, CAError errorCode, params object[] args)
		{
			if (_errorReported.HasValue && _errorReported.Value == ErrorTagging.ErrorType.Error) return;

			Statement.CodeAnalyzer.ReportError(span, errorCode, args);
			_errorReported = ErrorTagging.ErrorType.Error;
		}

		public void ReportWarning(Span span, CAError errorCode, params object[] args)
		{
			if (_errorReported.HasValue) return;

			Statement.CodeAnalyzer.ReportWarning(span, errorCode, args);
			_errorReported = ErrorTagging.ErrorType.Warning;
		}

		public ErrorTagging.ErrorType? ErrorReported
		{
			get { return _errorReported; }
			set { _errorReported = value; }
		}
	}
}
