﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class Statement
	{
		private CodeAnalyzer _ca;
		private GroupNode _root;
		private Span _span;

		public Statement(CodeAnalyzer ca)
		{
			_ca = ca;
			_root = new GroupNode(this);
		}

		public Statement(CodeAnalyzer ca, Span span)
		{
			_ca = ca;
			_root = new GroupNode(this);
			_span = span;
		}

		public static Statement Read(ReadParams p)
		{
			p.Code.SkipWhiteSpace();
			if (p.Code.EndOfFile) return null;

			var word = p.Code.PeekWordR();
			if (!string.IsNullOrEmpty(word))
			{
				switch (word)
				{
					case "break":
						return new BreakStatement(p, p.Code.MovePeekedSpan());
					case "col":
						return new ColStatement(p, p.Code.MovePeekedSpan());
					case "continue":
						return new ContinueStatement(p, p.Code.MovePeekedSpan());
					case "for":
						return new ForStatement(p, p.Code.MovePeekedSpan());
					case "format":
						return new FormatStatement(p, p.Code.MovePeekedSpan());
					case "header":
						return new HeaderStatement(p, p.Code.MovePeekedSpan());
					case "if":
						return new IfStatement(p, p.Code.MovePeekedSpan());
					case "return":
						return new ReturnStatement(p, p.Code.MovePeekedSpan());
					case "row":
						return new RowStatement(p, p.Code.MovePeekedSpan());
					case "switch":
						return new SwitchStatement(p, p.Code.MovePeekedSpan());
					case "while":
						return new WhileStatement(p, p.Code.MovePeekedSpan());
				}
			}

			var stmt = new Statement(p.CodeAnalyzer);
			p = p.Clone(stmt);

			while (!p.Code.EndOfFile)
			{
				if (p.Code.ReadExact(';')) return stmt;

				var node = ExpressionNode.Read(p);
				if (node == null) break;
				stmt.AddNode(node);
			}

			if (stmt._root.NumChildren == 0) return null;
			return stmt;
		}

		public bool IsEmpty
		{
			get { return _root.NumChildren == 0; }
		}

		public void AddNode(Node node)
		{
			_root.AddChild(node);
		}

		public virtual void Execute(RunScope scope)
		{
			if (scope.Returned == TriState.True ||
				scope.Breaked == TriState.True ||
				scope.Continued == TriState.True)
			{
				ReportError(Span, CAError.CA0016);	// Unreachable code.
			}

			if (_root.NumChildren > 0) _root.ReadValue(scope);
		}

		public CodeAnalyzer CodeAnalyzer
		{
			get { return _ca; }
		}

		public void ReplaceNodes(Node newNode, params Node[] oldNodes)
		{
			_root.ReplaceNodes(newNode, oldNodes);
		}

		public void ReportError(CodeModel.Span span, CAError errorCode, params object[] args)
		{
			CodeAnalyzer.ReportError(span, errorCode, args);
		}

		public CodeModel.Span Span
		{
			get
			{
				if (_span.IsEmpty) return _root.Span;
				else if (!_root.Span.IsEmpty) return _span.Envelope(_root.Span);
				else return _span;
			}
		}
	}
}
