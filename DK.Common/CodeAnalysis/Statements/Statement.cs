using DK.Code;
using DK.CodeAnalysis.Nodes;

namespace DK.CodeAnalysis.Statements
{
	abstract class Statement
	{
		private CodeAnalyzer _ca;
		
		private CodeSpan _span;

		public Statement(CodeAnalyzer ca)
		{
			_ca = ca;
		}

		public Statement(CodeAnalyzer ca, CodeSpan span)
		{
			_ca = ca;
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
					case "center":
						return new CenterStatement(p, p.Code.MovePeekedSpan());
					case "col":
					case "colff":
						return new ColStatement(p, p.Code.MovePeekedSpan());
					case "continue":
						return new ContinueStatement(p, p.Code.MovePeekedSpan());
					case "extract":
						return new ExtractStatement(p, p.Code.MovePeekedSpan());
					case "footer":
						return new FooterStatement(p, p.Code.MovePeekedSpan());
					case "for":
						return new ForStatement(p, p.Code.MovePeekedSpan());
					case "format":
						return new FormatStatement(p, p.Code.MovePeekedSpan());
					case "header":
						return new HeaderStatement(p, p.Code.MovePeekedSpan());
					case "if":
						return new IfStatement(p, p.Code.MovePeekedSpan());
					case "page":
						return new PageStatement(p, p.Code.MovePeekedSpan());
					case "return":
						return new ReturnStatement(p, p.Code.MovePeekedSpan());
					case "row":
						return new RowStatement(p, p.Code.MovePeekedSpan());
					case "select":
						return new SelectStatement(p, p.Code.MovePeekedSpan());
					case "switch":
						return new SwitchStatement(p, p.Code.MovePeekedSpan());
					case "while":
						return new WhileStatement(p, p.Code.MovePeekedSpan());
				}
			}

			var stmt = new SimpleStatement(p.CodeAnalyzer);
			p = p.Clone(stmt);

			while (!p.Code.EndOfFile)
			{
				if (p.Code.ReadExact(';')) return stmt;

				var node = ExpressionNode.Read(p, null);
				if (node == null) break;
				stmt.AddNode(node);
			}

			if (stmt.NumChildren == 0) return null;
			return stmt;
		}



		public virtual void Execute(CAScope scope)
		{
			if (scope.Returned == TriState.True ||
				scope.Breaked == TriState.True ||
				scope.Continued == TriState.True)
			{
				if (scope.UnreachableCodeReported != TriState.True)
				{
					ReportError(Span, CAError.CA0016);  // Unreachable code.
					scope.UnreachableCodeReported = TriState.True;
				}
			}
		}

		public CodeAnalyzer CodeAnalyzer
		{
			get { return _ca; }
		}

		public void ReportError(CodeSpan span, CAError errorCode, params object[] args)
		{
			CodeAnalyzer.ReportError(span, errorCode, args);
		}

		public CodeSpan Span
		{
			get { return _span; }
			set { _span = value; }
		}
	}
}
