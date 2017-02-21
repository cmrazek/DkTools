using System;
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
					case "if":
						return IfStatement.Read(p, p.Code.MovePeekedSpan());
					case "return":
						return ReturnStatement.Read(p, p.Code.MovePeekedSpan());
					case "while":
						return new WhileStatement(p, p.Code.MovePeekedSpan());
				}
			}

			var stmt = new Statement(p.CodeAnalyzer);
			p = p.Clone(stmt);

			while (!p.Code.EndOfFile)
			{
				if (p.Code.ReadExact(';')) return stmt;

				var node = ExpressionNode.Read(p, ";");
				if (node == null) break;
				stmt.AddNode(node);
			}

			return stmt;
		}

		public bool IsEmpty
		{
			get { return _root.NumChildren == 0; }
		}

		public void AddNode(Node node)
		{
			_root.AddNode(node);
		}

		public virtual void Execute(RunScope scope)
		{
			if (scope.Returned)
			{
				ReportError(Span, CAError.CA0016);	// Unreachable code.
			}

			_root.ReadValue(scope);
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
