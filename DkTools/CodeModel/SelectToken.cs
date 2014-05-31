using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class SelectToken : GroupToken
	{
		private SelectToken(GroupToken parent, Scope scope, KeywordToken selectToken)
			: base(parent, scope, new Token[] { selectToken })
		{
		}

		public static SelectToken Parse(GroupToken parent, Scope scope, KeywordToken selectToken)
		{
			var file = scope.File;
			var ret = new SelectToken(parent, scope, selectToken);

			var selectNameToken = StringLiteralToken.TryParse(ret, scope);
			if (selectNameToken != null) ret.AddToken(selectNameToken);

			var starToken = OperatorToken.TryParseMatching(ret, scope, "*");
			if (starToken != null) ret.AddToken(starToken);

			// Parse the table list until the where clause.
			Token token;
			if ((token = KeywordToken.TryParseMatching(ret, scope, "from")) != null)
			{
				var fromScope = scope;
				fromScope.Hint |= ScopeHint.SelectFrom | ScopeHint.SuppressControlStatements | ScopeHint.SuppressDataType | ScopeHint.SuppressFunctionCall | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl | ScopeHint.SuppressVars;

				var fromToken = new SelectFromToken(ret, fromScope, new Token[] { token });
				ret.AddToken(fromToken);

				fromToken.ParseScope(fromScope, t =>
					{
						if (t.BreaksStatement) return ParseScopeResult.StopAndReject;

						file.SkipWhiteSpaceAndComments(fromScope);
						if (file.PeekChar() == '{') return ParseScopeResult.StopAndKeep;
						switch (file.PeekWord())
						{
							case "where":
							case "order":
								return ParseScopeResult.StopAndKeep;
						}

						return ParseScopeResult.Continue;
					});
			}

			if ((token = KeywordToken.TryParseMatching(ret, scope, "where")) != null)
			{
				var whereScope = scope;
				whereScope.Hint |= ScopeHint.SelectWhereClause | ScopeHint.SuppressControlStatements | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

				var whereToken = new SelectWhereToken(ret, whereScope, new Token[] { token });
				ret.AddToken(whereToken);

				whereToken.ParseScope(whereScope, t =>
					{
						if (t.BreaksStatement) return ParseScopeResult.StopAndReject;

						file.SkipWhiteSpaceAndComments(whereScope);
						if (file.PeekChar() == '{' || file.PeekWord() == "order") return ParseScopeResult.StopAndKeep;

						return ParseScopeResult.Continue;
					});
			}

			if ((token = KeywordToken.TryParseMatching(ret, scope, "order")) != null)
			{
				var orderByScope = scope;
				orderByScope.Hint |= ScopeHint.SelectOrderBy | ScopeHint.SuppressControlStatements | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

				var orderByToken = new SelectOrderByToken(ret, orderByScope, new Token[] { token });
				ret.AddToken(orderByToken);

				if ((token = KeywordToken.TryParseMatching(ret, orderByScope, "by")) != null) orderByToken.AddToken(token);

				orderByToken.ParseScope(orderByScope, t =>
					{
						if (t.BreaksStatement) return ParseScopeResult.StopAndReject;

						file.SkipWhiteSpaceAndComments(orderByScope);
						if (file.PeekChar() == '{') return ParseScopeResult.StopAndKeep;

						return ParseScopeResult.Continue;
					});
			}

			var bodyScope = scope;
			bodyScope.Hint |= ScopeHint.SelectBody | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			if ((token = BracesToken.TryParse(ret, bodyScope)) != null)
			{
				ret.AddToken(token);
			}

			return ret;
		}
	}

	internal class SelectFromToken : GroupToken
	{
		public SelectFromToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, tokens)
		{ }
	}

	internal class SelectWhereToken : GroupToken
	{
		public SelectWhereToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, tokens)
		{ }
	}

	internal class SelectOrderByToken : GroupToken
	{
		public SelectOrderByToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, tokens)
		{ }
	}
}
