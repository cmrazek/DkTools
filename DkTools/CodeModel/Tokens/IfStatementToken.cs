using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class IfStatementToken : GroupToken
	{
		private List<Token> _condition = new List<Token>();
		private BracesToken _trueBody = null;
		private KeywordToken _elseToken = null;
		private Token _falseBody = null;

		private IfStatementToken(GroupToken parent, Scope scope, Position startPos)
			: base(parent, scope, startPos)
		{
		}

		public static IfStatementToken Parse(GroupToken parent, Scope scope, KeywordToken ifToken)
		{
			var file = scope.File;

			var ret = new IfStatementToken(parent, scope, file.Position);
			ret.AddToken(ifToken);

			var scopeIndent = scope.Clone();
			scopeIndent.Hint |= ScopeHint.NotOnRoot | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			// Condition
			ret.ParseScope(scopeIndent, t =>
				{
					if (t.BreaksStatement) return ParseScopeResult.StopAndReject;

					if (t is BracesToken)
					{
						ret._trueBody = t as BracesToken;
						return ParseScopeResult.StopAndKeep;
					}

					ret._condition.Add(t);
					return ParseScopeResult.Continue;
				});

			if (ret._trueBody != null)
			{
				var elseToken = KeywordToken.TryParseMatching(ret, scopeIndent, "else");
				if (elseToken != null)
				{
					ret.AddToken(ret._elseToken = elseToken);

					var falseBody = BracesToken.TryParse(ret, scopeIndent);
					if (falseBody != null) ret.AddToken(ret._falseBody = falseBody);
					else
					{
						var elseif = KeywordToken.TryParseMatching(ret, scope, "if");
						if (elseif != null) ret.AddToken(ret._falseBody = IfStatementToken.Parse(parent, scope, elseif));
					}
				}
			}

			return ret;
		}

		public override bool BreaksStatement
		{
			get
			{
				return true;
			}
		}
	}
}
