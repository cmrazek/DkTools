using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class ExtractStatement : GroupToken
	{
		private ExtractStatement(Scope scope, IEnumerable<Token> tokens)
			: base(scope)
		{
			foreach (var token in tokens) AddToken(token);
		}

		public static ExtractStatement Parse(Scope scope, KeywordToken extractKeywordToken)
		{
			var code = scope.Code;

			var tokens = new List<Token>();
			tokens.Add(extractKeywordToken);

			if (code.ReadExactWholeWord("permanent"))
			{
				var permToken = new KeywordToken(scope, code.Span, "permanent");
				tokens.Add(permToken);
			}

			ExtractTableDefinition exDef = null;

			if (!code.ReadWord())
			{
				// Not correct syntax; exit here.
				return new ExtractStatement(scope, tokens);
			}
			else if ((exDef = scope.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(code.Text).FirstOrDefault()) == null)
			{
				// The extract definition would have been added by the preprocessor. If it's not recognized here, then it's incorrect syntax.
				return new ExtractStatement(scope, tokens);
			}
			else
			{
				tokens.Add(new ExtractTableToken(scope, code.Span, code.Text, exDef));
			}

			var ret = new ExtractStatement(scope, tokens);
			var scope2 = scope.CloneIndent();
			scope2.Hint |= ScopeHint.SuppressControlStatements | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			while (!code.EndOfFile)
			{
				if (code.ReadExact(';'))
				{
					ret.AddToken(new StatementEndToken(scope2, code.Span));
					return ret;
				}

				var exp = ExpressionToken.TryParse(scope, null, (parseWord, parseWordSpan) =>
				{
					if (code.PeekExact('='))
					{
						var fieldDef = exDef.GetField(parseWord);
						if (fieldDef != null)
						{
							// Return the field token and the '='; otherwise the AssignmentOperator parsing will consume the next field's token.
							return new CompositeToken(scope, fieldDef.DataType,
								new ExtractFieldToken(scope, parseWordSpan, parseWord, fieldDef),
								new OperatorToken(scope, code.MovePeekedSpan(), code.Text));
						}
					}

					return null;
				});
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			return ret;
		}
	}
}
