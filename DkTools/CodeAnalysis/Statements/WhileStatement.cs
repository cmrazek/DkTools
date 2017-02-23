﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class WhileStatement : Statement
	{
		private ExpressionNode _cond;
		private List<Statement> _body = new List<Statement>();

		public WhileStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer)
		{
			p = p.Clone(this);

			_cond = ExpressionNode.Read(p, "{", ";");
			if (_cond == null)
			{
				ReportError(keywordSpan, CAError.CA0018, "if");	// Expected condition after '{0}'.
				return;
			}

			if (!p.Code.ReadExact("{"))
			{
				ReportError(keywordSpan, CAError.CA0019);	// Expected '{'.
				return;
			}

			while (!p.Code.EndOfFile)
			{
				while (!p.Code.EndOfFile && !p.Code.ReadExact("}"))
				{
					var stmt = Statement.Read(p);
					if (stmt == null) break;
					_body.Add(stmt);
				}
			}
		}

		public override void Execute(RunScope scope)
		{
			if (_cond != null) _cond.ReadValue(scope.Clone(dataTypeContext: DataType.Int));

			foreach (var stmt in _body)
			{
				stmt.Execute(scope);
			}
		}
	}
}