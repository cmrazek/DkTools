using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class WhileStatementToken : GroupToken
	{
		private Token[] _expressionTokens;
		private BracesToken _bodyToken;	// Could be null for unfinished code.

		private WhileStatementToken(GroupToken parent, Scope scope, KeywordToken whileToken)
			: base(parent, scope, new Token[] { whileToken })
		{
		}

		public static WhileStatementToken Parse(GroupToken parent, Scope scope, KeywordToken whileToken)
		{
			var file = scope.File;
			var ret = new WhileStatementToken(parent, scope, whileToken);

			// Expression
			var expressionScope = scope.Clone();
			expressionScope.Hint |= ScopeHint.SuppressDataType | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			var expressionTokens = new List<Token>();
			ret.ParseScope(expressionScope, t =>
				{
					if (t.BreaksStatement || t is BracesToken) return ParseScopeResult.StopAndReject;
					expressionTokens.Add(t);

					file.SkipWhiteSpaceAndComments(expressionScope);
					return file.PeekChar() == '{' ? ParseScopeResult.StopAndKeep : ParseScopeResult.Continue;
				});
			ret._expressionTokens = expressionTokens.ToArray();

			// Body
			var bodyScope = scope.Clone();
			bodyScope.Hint |= ScopeHint.SuppressFunctionDefinition;

			if ((ret._bodyToken = BracesToken.TryParse(parent, bodyScope)) != null) ret.AddToken(ret._bodyToken);

			return ret;
		}
	}
}
