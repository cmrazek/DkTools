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
			: base(stmt, funcNameSpan)
		{
			_name = funcName;
		}

		public static AggregateFunctionCallNode Read(ReadParams p, Span funcNameSpan, string funcName)
		{
			var ret = new AggregateFunctionCallNode(p.Statement, funcNameSpan, funcName);
			var code = p.Code;

			if (funcName == "count")
			{
				if (!code.ReadExact('*'))
				{
					ret.ReportError(CAError.CA0060);	// Expected '*' in count().
					return ret;
				}
			}
			else
			{
				var exp = ExpressionNode.Read(p, ",", ")");
				if (exp == null)
				{
					ret.ReportError(CAError.CA0061);	// Expected aggregate expression.
					return ret;
				}
				ret._aggExp = exp;
			}

			while (!code.EndOfFile)
			{
				if (code.ReadExact(')')) break;

				if (code.ReadExact(','))
				{
					if (code.ReadExactWholeWord("where"))
					{
						var exp = ExpressionNode.Read(p, ",", ")");
						if (exp == null)
						{
							ret.ReportError(code.Span, CAError.CA0062, "where");	// Expected expression to follow '{0}'.
							return ret;
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
								ret.ReportError(code.Span, CAError.CA0064, code.Text);	// Table '{0}' does not exist.
								return ret;
							}

							if (!code.ReadExact('.'))
							{
								ret.ReportError(code.Span, CAError.CA0066);	// Expected '.'
								return ret;
							}

							Definition fieldDef = null;
							if (!code.ReadWord() || (fieldDef = tableDef.GetChildDefinitions(code.Text).FirstOrDefault()) == null)
							{
								ret.ReportError(code.Span, CAError.CA0067); // Expected column name.
								return ret;
							}
						}
						else
						{
							ret.ReportError(code.Span, CAError.CA0065);	// Expected table name to follow 'group'.
							return ret;
						}
					}
					else if (code.ReadExactWholeWord("all"))
					{
						// Nothing follows 'all'
					}
					else if (code.ReadExactWholeWord("in"))
					{
						if (!code.ReadStringLiteral())
						{
							ret.ReportError(code.Span, CAError.CA0068);	// Expected select name to follow 'in'.
							return ret;
						}
					}
				}
				else
				{
					ret.ReportError(CAError.CA0063);	// Expected ')'.
				}
			}

			return ret;
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
	}
}
