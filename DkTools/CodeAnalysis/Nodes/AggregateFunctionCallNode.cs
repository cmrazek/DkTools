using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis.Nodes
{
	class AggregateFunctionCallNode : Node
	{
		private string _name;
		private ExpressionNode _aggExp;
		private ExpressionNode _whereExp;

		private AggregateFunctionCallNode(Statement stmt, Span funcNameSpan, string funcName)
			: base(stmt, null, funcNameSpan)
		{
			_name = funcName;
		}

		public override string ToString() => new string[] { _name, "(", _aggExp?.ToString(), _whereExp != null ? ", " : "", _whereExp?.ToString(), ")" }.Combine();

		public static AggregateFunctionCallNode Read(ReadParams p, Span funcNameSpan, string funcName)
		{
			var ret = new AggregateFunctionCallNode(p.Statement, funcNameSpan, funcName);
			var code = p.Code;

			if (funcName == "count")
			{
				if (code.ReadExact('*'))
				{
					code.ReadExact(',');
				}
			}
			else
			{
				var exp = ExpressionNode.Read(p, null, ",", ")");
				if (exp == null)
				{
					ret.ReportError(CAError.CA0061);	// Expected aggregate expression.
					return ret;
				}
				ret._aggExp = exp;
			}

			var closedPos = -1;

			while (!code.EndOfFile)
			{
				if (code.ReadExact(')'))
				{
					closedPos = code.Span.End;
					break;
				}

				if (code.ReadExact(','))
				{
					continue;
				}

				if (code.ReadExactWholeWord("where"))
				{
					var exp = ExpressionNode.Read(p, null, ",", ")");
					if (exp == null)
					{
						ret.ReportError(code.Span, CAError.CA0062, "where");    // Expected expression to follow '{0}'.
						break;
					}
					ret._whereExp = exp;
				}
				else if (code.ReadExactWholeWord("group"))
				{
					var startPos = code.Position;
					if (code.ReadWord())
					{
						var tableDef = (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetGlobalFromAnywhere(code.Text)
										where d is TableDefinition || d is ExtractTableDefinition
										select d).FirstOrDefault();
						if (tableDef == null)
						{
							ret.ReportError(code.Span, CAError.CA0064, code.Text);  // Table '{0}' does not exist.
							break;
						}

						if (!code.ReadExact('.'))
						{
							ret.ReportError(code.Span, CAError.CA0066); // Expected '.'
							break;
						}

						Definition fieldDef = null;
						if (!code.ReadWord() || (fieldDef = tableDef.GetChildDefinitions(code.Text, p.AppSettings).FirstOrDefault()) == null)
						{
							ret.ReportError(code.Span, CAError.CA0067); // Expected column name.
							break;
						}
					}
					else
					{
						ret.ReportError(code.Span, CAError.CA0065); // Expected table name to follow 'group'.
						break;
					}
				}
				else if (code.ReadExactWholeWord("all"))
				{
					// Nothing follows 'all'
				}
				else if (code.ReadExactWholeWord("in"))
				{
					if (!code.ReadStringLiteral() && !code.ReadWord())
					{
						ret.ReportError(code.Span, CAError.CA0068); // Expected select name to follow 'in'.
						break;
					}
				}
				else
				{
					ret.ReportError(CAError.CA0063);    // Expected ')'.
					break;
				}
			}

			if (closedPos < 0) closedPos = code.Position;
			ret.Span = new Span(funcNameSpan.Start, closedPos);

			return ret;
		}

		public override void Execute(RunScope scope)
		{
			if (_whereExp != null)
			{
				_whereExp.Execute(scope);
			}

			if (_aggExp != null)
			{
				_aggExp.ReadValue(scope);
			}
		}

		public override Values.Value ReadValue(RunScope scope)
		{
			if (_whereExp != null)
			{
				_whereExp.ReadValue(scope);
			}

			if (_aggExp != null)
			{
				return _aggExp.ReadValue(scope);
			}
			else if (_name == "count")
			{
				return Value.CreateUnknownFromDataType(DataType.Int);
			}
			else
			{
				return base.ReadValue(scope);
			}
		}

		public override bool IsReportable
		{
			get
			{
				if (_name == "count") return true;
				if (_aggExp != null) return _aggExp.IsReportable;
				return false;
			}
		}
	}
}
