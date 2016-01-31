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
		private ExtractStatement(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, tokens)
		{ }

		public static ExtractStatement Parse(GroupToken parent, Scope scope, KeywordToken extractKeywordToken)
		{
			var file = scope.File;

			var tokens = new List<Token>();
			tokens.Add(extractKeywordToken);

			ExtractTableToken exToken = null;
			ExtractTableDefinition exDef = null;

			file.SkipWhiteSpaceAndComments(scope);
			var word = file.PeekWord();
			if (word == "permanent")
			{
				var permToken = new KeywordToken(parent, scope, file.MoveNextSpan("permanent".Length), "permanent");
				tokens.Add(permToken);
				file.SkipWhiteSpaceAndComments(scope);
				word = file.PeekWord();
			}

			if (!string.IsNullOrEmpty(word) &&
				(exDef = scope.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(word).FirstOrDefault()) != null)
			{
				exToken = new ExtractTableToken(parent, scope, file.MoveNextSpan(word.Length), word, exDef);
				tokens.Add(exToken);
				file.SkipWhiteSpaceAndComments(scope);
			}
			else
			{
				// Not correct syntax; exit here.
				return new ExtractStatement(parent, scope, tokens);
			}

			var ret = new ExtractStatement(parent, scope, tokens);
			var scope2 = scope.CloneIndent();
			scope2.Hint |= ScopeHint.SuppressControlStatements | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			while (file.SkipWhiteSpaceAndComments(scope2))
			{
				if (file.IsMatch(';'))
				{
					ret.AddToken(new StatementEndToken(ret, scope2, file.MoveNextSpan()));
					return ret;
				}

				var exp = ExpressionToken.TryParse(ret, scope, null, (parseWord, parseWordSpan) =>
				{
					file.SkipWhiteSpaceAndComments(scope);
					if (file.IsMatch('='))
					{
						var fieldDef = exDef.GetField(parseWord);
						if (fieldDef != null)
						{
							return new ExtractFieldToken(ret, scope, parseWordSpan, parseWord, fieldDef);
						}
					}

					return null;
				});
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			return ret;

			// TODO: remove
			//// Read all the tokens into a list that we can navigate later.
			//tokens.Clear();
			//var savePos = file.Position;
			//while (true)
			//{
			//	var tok = file.ParseComplexToken(ret, scope2);
			//	if (tok == null) break;
			//	if (tok is StatementEndToken)
			//	{
			//		tokens.Add(tok);
			//		break;
			//	}
			//	if (tok.BreaksStatement)
			//	{
			//		file.Position = savePos;
			//		break;
			//	}

			//	tokens.Add(tok);
			//	savePos = file.Position;
			//}

			//// In the list, find tokens that occur before '=' and have a name matching an extract field.
			//// Replace the ones found with ExtractFieldTokens.
			//ExtractFieldDefinition fieldDef;
			//for (int i = 1, ii = tokens.Count; i < ii; i++)
			//{
			//	var tok = tokens[i];
			//	if (tok is OperatorToken && tok.Text == "=")
			//	{
			//		var prevTok = tokens[i - 1];
			//		if (prevTok is WordToken && (fieldDef = exDef.GetField(prevTok.Text)) != null)
			//		{
			//			tokens[i - 1] = new ExtractFieldToken(ret, scope2, prevTok.Span, fieldDef.Name, fieldDef);
			//		}
			//	}
			//}

			//ret.AddTokens(tokens);
			//return ret;
		}
	}
}
