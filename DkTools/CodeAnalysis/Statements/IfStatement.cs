using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class IfStatement : Statement
	{
		private ExpressionNode _cond;
		private List<Statement> _trueBody;
		private List<Statement> _falseBody;

		public IfStatement(CodeAnalyzer ca, Span keywordSpan)
			: base(ca, keywordSpan)
		{
		}

		public static IfStatement Read(ReadParams p, Span keywordSpan)
		{
			var ret = new IfStatement(p.CodeAnalyzer, keywordSpan);
			p = p.Clone(ret);
			var code = p.Code;

			var condition = ExpressionNode.Read(p, "{", ";");
			if (condition == null)
			{
				ret.ReportError(keywordSpan, CAError.CA0018, "if");	// Expected condition after '{0}'.
			}
			else
			{
				ret.AddNode(condition);
				ret.ConditionNode = condition;

				if (!code.ReadExact("{"))
				{
					ret.ReportError(keywordSpan, CAError.CA0019);	// Expected '{'.
				}
				else
				{
					while (!code.EndOfFile && !code.ReadExact("}"))
					{
						var stmt = Statement.Read(p);
						if (stmt == null) break;
						ret.AddTrueStatement(stmt);
					}

					if (code.ReadExactWholeWord("else"))
					{
						if (!code.ReadExact("{"))
						{
							ret.ReportError(keywordSpan, CAError.CA0019);	// Expected '{'.
						}
						else
						{
							while (!code.EndOfFile && !code.ReadExact("}"))
							{
								var stmt = Statement.Read(p);
								if (stmt == null) break;
								ret.AddFalseStatement(stmt);
							}
						}
					}
				}
			}

			return ret;
		}

		public ExpressionNode ConditionNode
		{
			get { return _cond; }
			set { _cond = value; }
		}

		public void AddTrueStatement(Statement stmt)
		{
			if (_trueBody == null) _trueBody = new List<Statement>();
			_trueBody.Add(stmt);
		}

		public void AddFalseStatement(Statement stmt)
		{
			if (_falseBody == null) _falseBody = new List<Statement>();
			_falseBody.Add(stmt);
		}

		public override void Execute(RunScope scope)
		{
			if (_cond != null)
			{
				_cond.ReadValue(scope);
			}

			var trueScope = scope.Clone();
			if (_trueBody != null)
			{
				foreach (var stmt in _trueBody)
				{
					stmt.Execute(trueScope);
				}
			}

			var falseScope = scope.Clone();
			if (_falseBody != null)
			{
				foreach (var stmt in _falseBody)
				{
					stmt.Execute(falseScope);
				}
			}

			scope.Merge(trueScope, falseScope);
		}
	}
}
