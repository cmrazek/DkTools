﻿using System;
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

		public virtual int Precedence
		{
			get { return 0; }
		}

		public virtual void Simplify(RunScope scope)
		{
			throw new NotImplementedException();
		}

		public virtual Value ReadValue(RunScope scope)
		{
			ReportError(Span, CAError.CA0103);	// Cannot read from this identifier.
			return Value.Void;
		}

		public virtual void WriteValue(RunScope scope, Value value)
		{
			ReportError(Span, CAError.CA0102);	// Cannot write to this identifier.
		}

		public virtual bool CanAssignValue(RunScope scope)
		{
			return false;
		}

		public virtual DataType DataType
		{
			get { return _dataType; }
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
