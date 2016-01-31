using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class StatementToken : GroupToken
	{
		private StatementToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, startPos)
		{
		}

		public delegate void StatementParseCallback(Token token);

		public static StatementToken TryParse(GroupToken parent, Scope scope, StatementParseCallback callback = null)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var ret = new StatementToken(parent, scope, file.Position);

			var word = file.PeekWord();
			if (!string.IsNullOrEmpty(word))
			{
				if (word == "if")
				{
					var keywordToken = new KeywordToken(ret, scope, file.MoveNextSpan(word.Length), word);
					var token = IfStatementToken.Parse(ret, scope, keywordToken);
					ret.AddToken(token);
					if (callback != null) callback(token);
					return ret;
				}
				else if (word == "switch")
				{
					var keywordToken = new KeywordToken(ret, scope, file.MoveNextSpan(word.Length), word);
					var token = SwitchToken.Parse(ret, scope, keywordToken);
					ret.AddToken(token);
					if (callback != null) callback(token);
					return ret;
				}
				else if (word == "while")
				{
					var keywordToken = new KeywordToken(ret, scope, file.MoveNextSpan(word.Length), word);
					var token = WhileStatementToken.Parse(ret, scope, keywordToken);
					if (callback != null) callback(token);
					return ret;
				}
			}

			var ch = file.PeekChar();
			if (ch == '#')
			{
				var startPos = file.Position;
				file.MoveNext();	// Skip #
				file.SeekNonWordChar();
				var wordSpan = new Span(startPos, file.Position);
				word = file.GetText(wordSpan);

				Token token;

				switch (word)
				{
					case "#insert":
						token = InsertToken.Parse(parent, scope, new InsertStartToken(parent, scope, wordSpan));
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
					case "#endinsert":
						token = new InsertEndToken(parent, scope, wordSpan);
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
					case "#replace":
						token = ReplaceToken.Parse(parent, scope, new ReplaceStartToken(parent, scope, wordSpan));
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
					case "#with":
						token = new ReplaceWithToken(parent, scope, wordSpan);
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
					case "#endreplace":
						token = new ReplaceEndToken(parent, scope, wordSpan);
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
					case "#include":
						token = IncludeToken.Parse(parent, scope, new PreprocessorToken(parent, scope, wordSpan, word));
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
					default:
						token = new PreprocessorToken(parent, scope, wordSpan, word);
						ret.AddToken(token);
						if (callback != null) callback(token);
						return ret;
				}
			}

			if (ch == '{')
			{
				// Start of a 'scope'. This is not allowed in PROBE/WBDK but allow it here anyway.
				var token = BracesToken.Parse(ret, scope);
				ret.AddToken(token);
				if (callback != null) callback(token);
				return ret;
			}
			else if (ch == '}')
			{
				// Unmatched '}'. This is a syntax error, but since it's a statement breaking token, add it here and end the statement.
				var token = new OperatorToken(ret, scope, file.MoveNextSpan(), "}");
				ret.AddToken(token);
				if (callback != null) callback(token);
				return ret;
			}

			var exp = ExpressionToken.TryParse(ret, scope, null);
			if (exp != null)
			{
				ret.AddToken(exp);
				if (callback != null) callback(exp);

				file.SkipWhiteSpaceAndComments(scope);
				ch = file.PeekChar();
			}

			if (ch == ';')
			{
				// Empty statement. This is not allowed in PROBE/WBDK, but allow it here anyway.
				var token = new StatementEndToken(ret, scope, file.MoveNextSpan(1));
				ret.AddToken(token);
				if (callback != null) callback(token);
				return ret;
			}

			return ret;
		}
	}
}
