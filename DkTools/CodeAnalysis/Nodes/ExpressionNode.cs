using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class ExpressionNode : GroupNode
	{
		public ExpressionNode(Statement stmt)
			: base(stmt)
		{
		}

		public override int Precedence
		{
			get
			{
				return 0;
			}
		}

		public static ExpressionNode Read(ReadParams p, params string[] stopStrings)
		{
			ExpressionNode exp = null;
			var code = p.Code;

			while (!code.EndOfFile)
			{
				if (stopStrings != null)
				{
					foreach (var str in stopStrings)
					{
						if (str.IsWord())
						{
							if (code.PeekExactWholeWord(str)) return exp;
						}
						else
						{
							if (code.PeekExact(str)) return exp;
						}
					}
				}

				if (!code.Read()) break;
				if (exp == null) exp = new ExpressionNode(p.Statement);

				switch (code.Type)
				{
					case CodeType.Number:
						exp.AddNode(new NumberNode(p.Statement, code.Span, code.Text));
						break;
					case CodeType.StringLiteral:
						exp.AddNode(new StringLiteralNode(p.Statement, code.Span, code.Text));
						break;
					case CodeType.Word:
						exp.AddNode(exp.ReadWord(p));
						break;
					case CodeType.Operator:
						switch (code.Text)
						{
							case "(":
							case "[":
								exp.AddNode(exp.ReadNestable(p, code.Span, code.Text, stopStrings));
								break;
							default:
								exp.AddNode(new OperatorNode(p.Statement, code.Span, code.Text));
								break;
						}
						break;
					default:
						exp.ReportError(code.Span, CAError.CA0001);	// Unknown '{0}'.
						exp.AddNode(new UnknownNode(p.Statement, code.Span, code.Text));
						break;
				}
			}

			return exp;
		}

		private Node ReadWord(ReadParams p)
		{
			var code = p.Code;
			var word = code.Text;
			var span = code.Span;

			if (code.ReadExact('('))
			{
				// This is a function call
				return FunctionCallNode.Read(p, span, word);
			}

			if (code.ReadExact('.'))
			{
				var dotSpan = code.Span;

				if (code.ReadWord())
				{
					var childWord = code.Text;
					var combinedWord = string.Concat(word, ".", childWord);
					var combinedSpan = span.Envelope(code.Span);

					if (code.ReadExact('('))
					{
						foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
												   where d.AllowsChild
												   select d))
						{
							var childDef = parentDef.ChildDefinitions.FirstOrDefault(c => c.Name == childWord && c.ArgumentsRequired);
							if (childDef != null)
							{
								return FunctionCallNode.Read(p, combinedSpan, combinedWord, childDef);
							}
						}

						ReportError(combinedSpan, CAError.CA0003, combinedWord);	// Function '{0}' not found.
						return new UnknownNode(p.Statement, combinedSpan, combinedWord);
					}
					else // No opening bracket
					{
						foreach (var parentDef in (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
												   where d.AllowsChild
												   select d))
						{
							var childDef = parentDef.ChildDefinitions.FirstOrDefault(c => c.Name == childWord && !c.ArgumentsRequired);
							if (childDef != null)
							{
								return new IdentifierNode(p.Statement, combinedSpan, combinedWord, childDef);
							}
						}

						ReportError(combinedSpan, CAError.CA0001, combinedWord);	// Unknown '{0}'.
						return new UnknownNode(p.Statement, combinedSpan, combinedWord);
					}
				}
				else // No word after dot
				{
					ReportError(dotSpan, CAError.CA0004);	// Expected identifier to follow '.'
					return new UnknownNode(p.Statement, span.Envelope(dotSpan), string.Concat(word, "."));
				}
			}
			else // No dot after word
			{
				var def = (from d in p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(code.Position + p.FuncOffset, word)
						   where !d.RequiresChild && !d.ArgumentsRequired
						   select d).FirstOrDefault();
				if (def != null) return new IdentifierNode(p.Statement, span, word, def);

				ReportError(span, CAError.CA0001, word);	// Unknown '{0}'.
				return new UnknownNode(p.Statement, span, word);
			}
		}

		private Node ReadNestable(ReadParams p, Span openSpan, string text, string[] stopStrings)
		{
			GroupNode groupNode;
			string endText;
			switch (text)
			{
				case "(":
					groupNode = new BracketsNode(p.Statement, openSpan);
					endText = ")";
					break;
				case "[":
					groupNode = new ArrayNode(p.Statement, openSpan);
					endText = "]";
					break;
				default:
					throw new ArgumentOutOfRangeException("text");
			}

			if (stopStrings == null) stopStrings = new string[] { endText };
			else stopStrings = stopStrings.Concat(new string[] { endText }).ToArray();

			while (!p.Code.EndOfFile)
			{
				if (p.Code.ReadExact(endText))
				{
					groupNode.Span = groupNode.Span.Envelope(p.Code.Span);
					break;
				}

				var exp = ExpressionNode.Read(p, stopStrings);
				if (exp == null) break;
				groupNode.AddNode(exp);
			}

			return groupNode;
		}
	}
}
